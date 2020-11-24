﻿namespace Ryujinx.Horizon.Sdk.OsTypes
{
    public struct EventType
    {
        public enum InitializatonState : byte
        {
            NotInitialized,
            Initialized
        }

        public bool Signaled;
        public bool InitiallySignaled;
        public byte ClearMode;
        public InitializatonState State;
        public ulong BroadcastCounter;
    }
}