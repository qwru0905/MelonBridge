// IL2CPP 환경에서는 게임의 생성 어셈블리가 이 타입들을 실제로 제공한다.
// Mono 환경에서는 이 파일의 타입들이 참조만 되고 실행되지 않는다.
namespace Il2CppSystem
{
    public class Object
    {
        public override string ToString() => base.ToString();
    }
}

namespace UnhollowerRuntimeLib
{
    public static class ClassInjector
    {
        public static void RegisterTypeInIl2Cpp<T>() { }
    }
}
