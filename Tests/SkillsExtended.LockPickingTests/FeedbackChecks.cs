using System.Text.Json;
using SkillsExtended.Config.Skills;
using SkillsExtended.LockPicking;
using SkillsExtended.Skills.LockPicking;

internal static class FeedbackChecks
{
    public static void Run(Action<bool, string> check, string output)
    {
        var config = new LockPickingData();
        void Advance(PinLockEngine engine, float depth, float lift, float pressure, int frames = 90)
        {
            for (var i = 0; i < frames; i++) engine.Advance(PinLockEngine.StepSeconds, depth, lift, pressure > 0, pressure);
        }
        PinLockDefinition Definition() => new()
        {
            Heights = [.5f, .5f, .5f], Order = [0, 1, 2],
            Types = [PinType.Standard, PinType.Standard, PinType.Spool], Catches = [[], [], [.25f]],
        };
        void Preview(string name, PickSnapshot snapshot, float input = 0)
        {
            if (output == null) return;
            Directory.CreateDirectory(output);
            var view = new LockPickingCutaway();
            view.Render(snapshot, 0, true, commandLift: input);
            var mesh = view.CaptureMesh();
            File.WriteAllText(Path.Combine(output, name + ".json"), JsonSerializer.Serialize(new
            {
                vertices = mesh.Vertices.Select(v => new[] { v.Point.x, v.Point.y, v.Tint.r, v.Tint.g, v.Tint.b, v.Tint.a }),
                triangles = mesh.Triangles,
                selected = snapshot.Selected, pressure = snapshot.TensionStrength,
            }));
        }
        var engine = new PinLockEngine(Definition(), config, 1, 0);
        check(engine.Snapshot().PinMotion.All(p => p.KeyLift == 0 && p.DriverLift == 0),
            "Untouched pins disclose no target displacement");
        Preview("feedback-01-rest", engine.Snapshot());
        Advance(engine, .1f, 0, .35f);
        var applied = engine.Snapshot();
        check(applied.Cues.Count(c => c.Sound == PickSound.Tension) == 1, "Applying pressure produces one cue");
        Preview("feedback-02-pressure", applied);
        Advance(engine, .1f, .5f, .35f);
        var seated = engine.Snapshot();
        check(seated.Cues.Single(c => c.Sound == PickSound.Seat).Pin == 0,
            "Seating cue identifies the contacted pin");
        check(seated.PinMotion[0].DriverLift == seated.Lift && seated.PinMotion.Skip(1).All(p => p.DriverLift == 0),
            "Retained driver travel comes from observed contact, leaving untouched pins private");
        Preview("feedback-03-seating", seated, .5f);
        Advance(engine, .5f, 0, .35f);
        var retained = engine.Snapshot();
        check(retained.Selected == 1 && retained.PinMotion[0].KeyLift == 0 && retained.PinMotion[0].DriverLift == seated.Lift,
            "Leaving a set pin lowers the key pin while its driver stays supported");
        Preview("feedback-04-retained", retained);
        Advance(engine, .5f, .5f, .35f);
        Advance(engine, .9f, 0, .35f);
        Advance(engine, .9f, .25f, .35f);
        var falseSet = engine.Snapshot();
        check(falseSet.Cues.Single(c => c.Sound == PickSound.Catch).Pin == 2 && falseSet.CylinderRotation > .08f,
            "A spool catch has its own localized cue and physical false-set rotation");
        Preview("feedback-05-false-set", falseSet, .25f);
        Advance(engine, .9f, .5f, .65f, 20);
        check(engine.Snapshot().Strain > 0, "Forcing a trapped spool is visibly different from pressure alone");
        Preview("feedback-06-resistance", engine.Snapshot(), .5f);
        Advance(engine, .9f, .5f, .15f, 36);
        var counter = engine.Snapshot();
        check(counter.Cues.Count(c => c.Sound == PickSound.CounterRotation) == 1
            && counter.Cues.Single(c => c.Sound == PickSound.CounterRotation).Pin == 2,
            "A backward rotation produces one cue for the contacted spool");
        check(counter.Cues.Any(c => c.Sound == PickSound.Drop && c.Pin == 1)
            && counter.PinMotion[1].DriverLift == 0 && counter.PinMotion[0].DriverLift > 0,
            "Low-pressure counter-rotation drops only the driver that lost support");
        Preview("feedback-07-counter-rotation", counter, .5f);
        Advance(engine, .9f, 0, 0);
        var released = engine.Snapshot();
        check(released.PinMotion.All(p => p.KeyLift == 0 && p.DriverLift == 0)
            && released.Cues.Any(c => c.Sound == PickSound.Release), "Full release removes support from all pins");
        Preview("feedback-08-release", released);

        var overset = new PinLockEngine(Definition(), config, 1, 0);
        Advance(overset, .1f, .5f, .35f);
        Advance(overset, .1f, 1, .35f, 30);
        check(overset.Coaching().State == PinState.Overset, "Feedback recovery fixture reaches an actual overset");
        Advance(overset, .1f, 0, .20f);
        check(overset.Snapshot().Cues.Any(c => c.Sound == PickSound.Drop && c.Pin == 0)
            && overset.Snapshot().PinMotion[0].DriverLift == 0, "Overset recovery releases observed support");
        Advance(overset, .1f, 0, .8f);
        check(overset.Snapshot().Strain == 0 && overset.Wear == 0,
            "High pressure alone still causes neither strain nor wear");
        var adjustmentCount = overset.Snapshot().Cues.Count(c => c.Sound == PickSound.AdjustTension);
        Advance(overset, .1f, 0, .8f);
        check(overset.Snapshot().Cues.Count(c => c.Sound == PickSound.AdjustTension) == adjustmentCount,
            "Steady pressure does not repeatedly emit adjustment sounds");

        var clone = retained.PinMotion[0].DriverLift;
        retained.PinMotion[0].DriverLift = 1;
        check(engine.Snapshot().PinMotion[0].DriverLift == 0 && seated.PinMotion[0].DriverLift == clone,
            "Snapshots own their motion arrays independently");
        var packet = Newtonsoft.Json.JsonConvert.SerializeObject(new PickReply
        {
            ProtocolVersion = PickingProtocol.Version, State = counter, Coaching = engine.Coaching(),
        });
        var roundTrip = Newtonsoft.Json.JsonConvert.DeserializeObject<PickReply>(packet);
        check(JsonSerializer.Serialize(roundTrip.State) == JsonSerializer.Serialize(counter),
            "Fika serialization preserves all motion, pin identities and cue timestamps");
        check(PickingProtocol.Version == 7, "Mechanical-state snapshot change advances the picking protocol");
        var authority = new PickingAuthority(config, 123);
        var incompatible = authority.Process(new PickRequest
        {
            ProtocolVersion = 6, Raid = authority.Raid, Actor = "operator", Door = "door", Tool = "pick", Operation = "start",
        }, 0, 1, 0, 10, null);
        check(incompatible.Error == PickingProtocol.UpdateMessage && authority.Active.Count == 0,
            "Previous protocol is rejected before creating an attempt");
        var reader = new PickCueReader();
        var delivered = reader.Read(counter);
        check(delivered.Length > 0 && reader.Read(counter).Length == 0, "Repeated snapshots do not replay event cues");
        check(reader.Read(falseSet).Length == 0, "Older cue history does not rewind the reader");
        reader.Reset();
        check(reader.Read(counter).Length == delivered.Length, "New attempt explicitly resets cue delivery");
        check(!PickPresentation.Fresh(released, falseSet.Cues.Last()), "Old retained events expire before presentation");
        var longSnapshot = new PickSnapshot
        {
            Pins = 5, PinMotion = Enumerable.Range(0, 5).Select(_ => new PickPinMotion { KeyLift = .75f, DriverLift = .8f }).ToArray(),
            SetPinStates = new bool[5],
            SelectedPinState = PinState.Overset, FalseSet = true,
            Cues = Enumerable.Range(1, 32).Select(i => new PickCue { Sequence = int.MaxValue - i, Pin = 4, Time = 12345.123f, Sound = PickSound.CounterRotation }).ToArray(),
        };
        var largest = Newtonsoft.Json.JsonConvert.SerializeObject(new PickReply
        {
            ProtocolVersion = 7, Actor = new string('a', 128), Door = new string('d', 512),
            Attempt = Guid.NewGuid().ToString(), Raid = Guid.NewGuid().ToString(), State = longSnapshot,
            Coaching = new PickCoaching(),
        });
        check(System.Text.Encoding.UTF8.GetByteCount(largest) < 8192, "Five-pin snapshot with full cue history fits the Fika reply limit");
        check(Math.Abs(PickPresentation.RotationDegrees(.015f) - 1.35f) < .0001f
            && PickPresentation.RotationDegrees(.1f) == 9 && PickPresentation.RotationDegrees(1) == 90,
            "Both views retain physical working rotation instead of magnifying a false set to thirty degrees");

        string Geometry(LockPickingCutaway view) => JsonSerializer.Serialize(view.CaptureMesh().Vertices.Select(v =>
            new[] { v.Point.x, v.Point.y, v.Tint.r, v.Tint.g, v.Tint.b }));
        var animated = new LockPickingCutaway();
        var visual = new PickSnapshot { Pins = 3, Selected = 2 };
        animated.Render(visual, 0, false);
        var restingGeometry = Geometry(animated);
        visual.Cue = 1;
        visual.Cues = [new PickCue { Sequence = 1, Sound = PickSound.Seat, Pin = 0 }];
        animated.Render(visual, 0, false);
        var contactGeometry = Geometry(animated);
        check(contactGeometry != restingGeometry, "Localized seating pulse is visible even after selection moved away");
        animated.Render(visual, .4f, false);
        check(Geometry(animated) == restingGeometry, "Seating pulse expires without repeating retained history");
        animated.ResetAnimation();
        animated.Render(visual, 0, false);
        check(Geometry(animated) == restingGeometry, "Snapshot recovery restores pose without replaying old contact events");
        visual.Cue = 2;
        visual.Cues = [new PickCue { Sequence = 2, Sound = PickSound.Catch, Pin = 0 }];
        animated.Render(visual, 0, true);
        check(Geometry(animated) == restingGeometry, "Reduced motion removes transient seating bounce and emphasis");
        visual.Tension = true;
        visual.TensionStrength = .8f;
        animated.Render(visual, 0, true);
        check(Geometry(animated) != restingGeometry, "Reduced motion still communicates applied pressure");
        var recovered = new LockPickingCutaway();
        recovered.Render(counter, 0, true);
        animated.Render(counter, 0, true);
        check(Geometry(animated) == Geometry(recovered), "Current snapshot fully restores retained drivers after missed updates");

        for (var count = 3; count <= 5; count++)
        foreach (var pressure in new[] { 0f, .35f, 1f })
        foreach (var reduced in new[] { false, true })
        {
            var view = new LockPickingCutaway();
            var snapshot = new PickSnapshot
            {
                Pins = count, Selected = count - 1, Lift = 1, Tension = pressure > 0,
                TensionStrength = pressure, CylinderRotation = .1f, Strain = 1, Feedback = PickFeedback.Strain,
                PinMotion = Enumerable.Range(0, count).Select(_ => new PickPinMotion { KeyLift = 1, DriverLift = 1 }).ToArray(),
            };
            view.Render(snapshot, .016f, reduced, commandLift: 1);
            check(view.CaptureMesh().Vertices.All(v => float.IsFinite(v.Point.x) && float.IsFinite(v.Point.y)
                && Math.Abs(v.Point.x) <= 500 && Math.Abs(v.Point.y) <= 95),
                "Pressure, separate drivers and strain remain inside the compact cutaway bounds");
            foreach (var size in new[] { (1920, 1080), (2560, 1440), (3440, 1440), (5120, 1440) })
            {
                var scale = Math.Min(size.Item1 / 1920f, size.Item2 / 1080f);
                check(view.CaptureMesh().Vertices.All(v => Math.Abs(v.Point.x * scale) < size.Item1 / 2f
                    && Math.Abs((v.Point.y - 120) * scale) < size.Item2 / 2f),
                    "New feedback geometry remains visible at 1080p, 1440p and ultrawide");
            }
        }
    }
}
