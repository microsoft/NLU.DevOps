namespace NLU.DevOps.Luis
{
    /// <summary>
    /// Status for Model training
    /// </summary>
    public enum ModelTrainingStatus
    {
        /// <summary>
        /// Indicates there was a failure training the model
        /// </summary>
        Fail,

        /// <summary>
        /// Indicates that the model is still being trained or queued for training
        /// </summary>
        InProgress,

        /// <summary>
        /// Indicates successful training of the model
        /// </summary>
        Success
    }
}