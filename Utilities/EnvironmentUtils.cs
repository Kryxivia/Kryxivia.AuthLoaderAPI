using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kryxivia.AuthLoaderAPI.Utilities
{
    public static class EnvironmentUtils
    {
        public static bool BootstrapMongoDB()
        {
            bool.TryParse(Environment.GetEnvironmentVariable("BOOTSTRAP_MONGODB"), out bool bootstrapMongoDB);
            return bootstrapMongoDB;
        }
    }
}
