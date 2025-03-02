using System;
using SSMenuSystem.Features.Interfaces;
using UserSettings.ServerSpecific;

namespace SSMenuSystem.Features.Wrappers
{
    /// <summary>
    /// Initialize a new instance of wrapper for <see cref="SSTwoButtonsSetting"/>. This setting make a two buttons (like a checkbox). You can know with <see cref="SSTwoButtonsSetting.SyncIsA"/> (or <see cref="SSTwoButtonsSetting.SyncIsB"/>) if the button selectionned by the client is the first button or the second button.
    /// </summary>
    public class YesNoButton : SSTwoButtonsSetting, ISetting
    {
        /// <summary>
        /// The method that will be executed when the button is updated. It's contains three parameters: <br></br><br></br>
        /// - <see cref="ReferenceHub"/>, the player concerned by the change<br></br>
        /// - <see cref="bool"/>, where the value is basicaly the <see cref="SSTwoButtonsSetting.SyncIsA"/><br></br>
        /// - <see cref="SSTwoButtonsSetting"/>, where it's the class synced.
        /// </summary>
        /// <remarks>No errors will be thrown if <see cref="Action"/> is null.</remarks>
        public Action<ReferenceHub, bool, SSTwoButtonsSetting> Action { get; }

        /// <summary>
        /// The base instance (sent in to the client).
        /// </summary>
        public ServerSpecificSettingBase Base { get; private set; }

        /// <summary>
        /// Initialize a new instance of <see cref="YesNoButton"/>.
        /// </summary>
        /// <param name="id">The id of <see cref="SSTwoButtonsSetting"/>. value "null" and is not supported, even if you can set it to null, and it will result of client crash. </param>
        /// <param name="label">The label of <see cref="SSTwoButtonsSetting"/>. displayed at left in the row.</param>
        /// <param name="optionA">The content of the first button.</param>
        /// <param name="optionB">The content of the second button.</param>
        /// <param name="onChanged">Triggered when player changed the button (by clicking on the first or on the second).</param>
        /// <param name="defaultIsB">Is, if the player doesn't have an override value saved, the B is selected by default.</param>
        /// <param name="hint">The hint (located in "?"). If null, no hint will be displayed.</param>
        public YesNoButton(int? id, string label, string optionA, string optionB, Action<ReferenceHub, bool, SSTwoButtonsSetting> onChanged = null, bool defaultIsB = false, string hint = null) : base(id, label, optionA, optionB, defaultIsB, hint)
        {
            Base = new SSTwoButtonsSetting(id, label, optionA, optionB, defaultIsB, hint);
            Action = onChanged;
        }
    }
}