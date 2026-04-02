using GameEngine;
using GameNative;
using GameProto;

namespace GameLogic
{
    public class GameService
    {
        public string Run()
        {
            var engine = new EngineCore();
            var native = new NativeAdapter();
            var message = new GameMessage { Id = 1, Content = "Hello Unity" };
            return $"{engine.GetEngineName()} + {native.GetPlatformName()} + {message.Content}";
        }
    }
}
