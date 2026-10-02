namespace SkillsExtended.DeveloperTools;

// Once RMB owns the camera, the locked cursor's position must not hand control
// back to UI as scene labels or panels pass beneath the centre of the screen.
internal sealed class DeveloperEditorCameraInput
{
    public bool Looking { get; private set; }

    public void Update(bool rightMouseHeld, bool pointerOverPanel) =>
        Looking = rightMouseHeld && (Looking || !pointerOverPanel);

    public void Reset() => Looking = false;
}
