using System;

namespace HLTools
{
    [Serializable]
    public class TextureDimensionException : Exception
    {
        public TextureDimensionException() : base() { }
        public TextureDimensionException(string message) : base(message) { }
        public TextureDimensionException(string message, Exception inner) : base(message, inner) { }

        protected TextureDimensionException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext)
        {
            throw new NotImplementedException();
        }
    }
}
