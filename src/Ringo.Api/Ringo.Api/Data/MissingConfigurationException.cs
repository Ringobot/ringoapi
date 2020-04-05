using System;

namespace Ringo.Api.Data
{
    public class MissingConfigurationException : Exception
    {
        public MissingConfigurationException(string configKey) : base($"App Setting \"{configKey}\" is missing.")
        {
        }
    }
}
