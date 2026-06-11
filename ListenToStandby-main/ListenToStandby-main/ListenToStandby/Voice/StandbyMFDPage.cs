using UnityEngine;
using UnityEngine.Events;

namespace ListenToStandby.Voice
{
    public class StandbyMFDPage : MFDPage
    {
        private void Start()
        {
            pageName = "STBY";
            SetText("STBY", "MFD Radio Page");
            SetPageButtons(new[]
            {
                new MFDPage.MFDButtonInfo { button = MFD.MFDButtons.L1, label = "Enable", OnPress = new UnityEvent() },
                new MFDPage.MFDButtonInfo { button = MFD.MFDButtons.L2, label = "Cycle", OnPress = new UnityEvent() },
                new MFDPage.MFDButtonInfo { button = MFD.MFDButtons.R1, label = "Slot 1", OnPress = new UnityEvent() },
                new MFDPage.MFDButtonInfo { button = MFD.MFDButtons.R2, label = "Slot 2", OnPress = new UnityEvent() },
                new MFDPage.MFDButtonInfo { button = MFD.MFDButtons.R3, label = "Slot 3", OnPress = new UnityEvent() },
                new MFDPage.MFDButtonInfo { button = MFD.MFDButtons.R4, label = "Slot 4", OnPress = new UnityEvent() },
            });
        }
    }
}
