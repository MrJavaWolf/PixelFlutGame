using System.Numerics;
namespace StickFigureGame;

public interface IStickFigureInput
{
    public bool GetSlashAttackInput();
    public bool GetSlashAttackInputOnDown();
    public bool GetShootAttackInput();
    public bool GetDashInput();
    public bool GetJumpInput();
    public Vector2 GetInput();
}


public class StickFigureInput2 : IStickFigureInput
{
    public bool GetSlashAttackInput()
    {
        return Input.GetKey(KeyCode.Joystick1Button0);
    }

    public bool GetSlashAttackInputOnDown()
    {
        return Input.GetKeyDown(KeyCode.Joystick1Button0);
    }

    public bool GetShootAttackInput()
    {
        return Input.GetKey(KeyCode.Joystick1Button3);
    }

    public bool GetDashInput()
    {
        return Input.GetKey(KeyCode.Joystick1Button2);
    }

    public bool GetJumpInput()
    {
        return Input.GetKey(KeyCode.Joystick1Button1);
    }

    public Vector2 GetInput()
    {
        return new Vector2(
            Input.GetAxis("Horizontal"),
            Input.GetAxis("Vertical"));
    }
}

public class StickFigureInput : IStickFigureInput
{

    public bool GetSlashAttackInput()
    {
        return Input.GetKey(KeyCode.K);
    }

    public bool GetSlashAttackInputOnDown()
    {
        return Input.GetKeyDown(KeyCode.K);
    }

    public bool GetShootAttackInput()
    {
        return Input.GetKey(KeyCode.L);
    }

    public bool GetDashInput()
    {
        return Input.GetKey(KeyCode.LeftShift);
    }

    public bool GetJumpInput()
    {
        return Input.GetKey(KeyCode.Space);
    }

    public Vector2 GetInput()
    {
        float xMovment = 0;
        if (Input.GetKey(KeyCode.A) && Input.GetKey(KeyCode.D))
        {
            xMovment = 0;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            xMovment = -1;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            xMovment = 1;
        }
        float yMovment = 0;
        if (Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.S))
        {
            yMovment = 0;
        }
        else if (Input.GetKey(KeyCode.W))
        {
            yMovment = 1;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            yMovment = -1;
        }
        Vector2 movement = new Vector2(xMovment, yMovment);
        return movement;
    }
}
