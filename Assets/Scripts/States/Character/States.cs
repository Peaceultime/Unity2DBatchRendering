public class CharacterIdleState : Singleton<CharacterIdleState>, IState
{
    public void Enter()
    { }

    public void Exit()
    { }

    public IState Update()
    {
        //If Move button in the UI or the keyboard button is pressed, return the instance of CharacterMoveState

        //If the player ends his turn, return the instance of CharacterNotPlayingState

        return null;
    }
}

public class CharacterMoveState : Singleton<CharacterMoveState>, IState
{
    public void Enter()
    { 
        //Change the UI to show the current available range
    }

    public void Exit()
    { 
        //Hide the range
        //Register the target position for the character
    }

    public IState Update()
    {
        //If the confirm button is pressed on a valid hexagon, return the instance of CharacterAnimatingMovementState
        return null;
    }
}

public class CharacterAnimatingMovementState : Singleton<CharacterAnimatingMovementState>, IState
{
    public void Enter()
    { 
        //Runs the job for the movement animation and the fog of war animation
    }

    public void Exit()
    {  }

    public IState Update()
    {
        //When all the jobs are finished, returns the instance of CharacterIdleState
        return null;
    }
}

public class CharacterNotPlayingState : Singleton<CharacterNotPlayingState>, IState
{
    public void Enter()
    { 
        //Starts the AI job of the other NPCs
    }

    public void Exit()
    { }

    public IState Update()
    {
        //When all the AI jobs are finished, returns the instance of CharacterIdleState
        return null;
    }
}