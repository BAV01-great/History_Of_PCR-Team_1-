namespace PCR
{
    /// <summary>Anything the player can aim at and press: HoloButton, LabInteractable. The desktop rig and the XR events drive this.</summary>
    public interface IPressable
    {
        void SetHover(bool hovered);
        void Press();
    }
}
