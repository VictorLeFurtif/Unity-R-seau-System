using Unity.Collections;
using Unity.Netcode;

namespace Struct
{
    public struct PlayerData : INetworkSerializable
    {
        public int life;
        public bool stunt;
        public FixedString128Bytes message;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref life);    
            serializer.SerializeValue(ref stunt);    
            serializer.SerializeValue(ref message);    
        }
    }
}