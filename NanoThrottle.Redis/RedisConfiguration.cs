using StackExchange.Redis;

namespace NanoThrottle.Redis
{
    public class RedisConfiguration
    {
        public RedisConfiguration(string connectionString)
            : this(ConfigurationOptions.Parse(connectionString))
        { }

        public RedisConfiguration(ConfigurationOptions configurationOptions)
        {
            ConfigurationOptions = configurationOptions;
        }
        
        public ConfigurationOptions ConfigurationOptions { get; }

        public string ConnectionString => ConfigurationOptions.ToString();

        public override string ToString() => ConnectionString;

        public static implicit operator RedisConfiguration(string connectionString)
        {
            return new RedisConfiguration(connectionString);
        }

        public static implicit operator ConfigurationOptions(RedisConfiguration redisConfiguration)
        {
            return redisConfiguration.ConfigurationOptions;
        }
    }
}