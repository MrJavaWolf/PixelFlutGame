namespace PixelFlut.Core
{
    public class GamepadButton
    {
        /// <summary>
        /// Is true if the button is currently pressed down, otherwise false.
        /// </summary>
        public bool IsPressed { get; private set; } = false;

        /// <summary>
        /// Is true if the button was pressed down this frame(/update), otherwise false.
        /// Use this if you want to trigger a event when the button was pressed.
        /// </summary>
        public bool OnPress { get; private set; } = false;

        /// <summary>
        /// Is true if the button was released this frame(/update), otherwise false.
        /// Use this if you want to trigger a event when the button was released.
        /// </summary>
        public bool OnRelease { get; private set; } = false;

        /// <summary>
        /// Update the button to a new state
        /// </summary>
        /// <param name="isPressedDown"></param>
        public void Loop(bool isPressedDown)
        {
            // Was not pressed down before, is pressed down now
            if (!IsPressed && isPressedDown)
            {
                OnPress = true;
                OnRelease = false;
            }

            // Is keeping it pressed down
            else if(IsPressed && isPressedDown)
            {
                OnPress = false;
                OnRelease = false;
            }

            // Releases the button
            else if (IsPressed && !isPressedDown)
            {
                OnPress = false;
                OnRelease = true;
            }

            // Was not pressed and is still not pressed
            else if (!IsPressed && !isPressedDown)
            {
                OnPress = false;
                OnRelease = false;
            }

            IsPressed = isPressedDown;
        }
    }
}
