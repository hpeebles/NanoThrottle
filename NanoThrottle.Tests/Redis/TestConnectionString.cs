using System;

namespace NanoThrottle.Tests.Redis
{
    public static class TestConnectionString
    {
        public static readonly string Value = $"{Endpoint},password={Password}";

        private static string Endpoint => Environment.GetEnvironmentVariable("RedisTestEndpoint");
        private static string Password => Environment.GetEnvironmentVariable("RedisTestPassword");
    }
}