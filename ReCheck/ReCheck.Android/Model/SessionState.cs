namespace ReCheck.Model
{
    public enum SessionState
    {
        NotStarted,
        BeginResetBaseline,
        WaitResetBaseline,
        FinishResetBaseline,
        SessionRunning,
        ErrorDetected,
        DeviceMissing,
        SetupFailed
    }
}