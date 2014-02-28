namespace Mina.Filter.Logging
{
    /// <summary>
    /// Defines a logging level.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Not log any information
        /// </summary>
        None,
        /// <summary>
        /// Logs messages on the ERROR level.
        /// </summary>
        Error,
        /// <summary>
        /// Logs messages on the WARN level.
        /// </summary>
        Warn,
        /// <summary>
        /// Logs messages on the INFO level.
        /// </summary>
        Info,
        /// <summary>
        /// Logs messages on the DEBUG level.
        /// </summary>
        Debug,
        /// <summary>
        /// Logs messages on the TRACE level.
        /// </summary>
        Trace
    }
}
