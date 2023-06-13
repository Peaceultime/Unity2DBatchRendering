public class StateMachine
{
	public IState currentState;

	public StateMachine(IState initialState)
    {
		currentState = initialState;
		currentState.Enter();
    }
	public void Update()
	{
		IState nextState = currentState.Update();
		if (nextState != null)
		{
			currentState.Exit();
			currentState = nextState;
			currentState.Enter();
		}
	}
}
public interface IState
{
	public IState Update();
	public void Enter();
	public void Exit();
}