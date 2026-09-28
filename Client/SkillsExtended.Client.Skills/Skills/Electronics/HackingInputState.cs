using EFT;

namespace SkillsExtended.Skills.Electronics;

/// <summary>Only a raid with a local player may enable EFT's player-dependent input flags.</summary>
public sealed class HackingInputState
{
    private bool _captured;
    private bool _ignore;
    private bool _dialog;

    public void Capture(bool inRaid)
    {
        if (_captured || !inRaid || GamePlayerOwner.MyPlayer == null)
        {
            return;
        }

        _ignore = GamePlayerOwner.IgnoreInputWithKeepResetLook;
        _dialog = GamePlayerOwner.IgnoreInputInNPCDialog;
        _captured = true;
        GamePlayerOwner.IgnoreInputWithKeepResetLook = true;
        GamePlayerOwner.IgnoreInputInNPCDialog = true;
    }

    public void Restore()
    {
        if (!_captured)
        {
            return;
        }

        _captured = false;
        // A disappearing player must never leave NPC-dialog input enabled at the menu.
        var hasPlayer = GamePlayerOwner.MyPlayer != null;
        GamePlayerOwner.IgnoreInputWithKeepResetLook = hasPlayer && _ignore;
        GamePlayerOwner.IgnoreInputInNPCDialog = hasPlayer && _dialog;
    }
}
