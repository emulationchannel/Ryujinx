using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Kernel;
using Ryujinx.Horizon.Kernel.Common;
using Ryujinx.Horizon.Kernel.Svc;

namespace Ryujinx.HLE.HOS.Services
{
    static class Map
    {
        public struct AddressSpaceRegion
        {
            public ulong Base;
            public ulong Size;
            public ulong End;
        }

        public struct AddressSpaceInfo
        {
            public AddressSpaceRegion Heap;
            public AddressSpaceRegion Alias;
            public AddressSpaceRegion Aslr;
        }

        private const int CurrentProcessHandle = unchecked((int)0xffff8001);

        public static Result LocateMappableSpace(out ulong address, ulong size)
        {
            address = 0;

            Result result = GetAddressSpaceInfo(out AddressSpaceInfo asInfo);

            if (result != Result.Success)
            {
                return result;
            }

            ulong currentBase = asInfo.Aslr.Base;
            ulong currentEnd = currentBase + size;

            if (currentBase >= currentEnd)
            {
                return KernelResult.OutOfMemory;
            }

            while (true)
            {
                if (asInfo.Heap.Size != 0 && (asInfo.Heap.Base <= currentEnd - 1 && currentBase <= asInfo.Heap.End - 1))
                {
                    if (currentBase == asInfo.Heap.End)
                    {
                        return KernelResult.OutOfMemory;
                    }

                    currentBase = asInfo.Heap.End;
                }
                else if (asInfo.Alias.Size != 0 && (asInfo.Alias.Base <= currentEnd - 1 && currentBase <= asInfo.Alias.End - 1))
                {
                    if (currentBase == asInfo.Alias.End)
                    {
                        return KernelResult.OutOfMemory;
                    }

                    currentBase = asInfo.Alias.End;
                }
                else
                {
                    result = KernelStatic.Syscall.QueryMemory(out MemoryInfo info, currentBase);

                    // TODO: Abort on failure above.

                    if (info.State == 0 && info.Address - currentBase + info.Size >= size)
                    {
                        address = currentBase;
                        return Result.Success;
                    }

                    if (currentBase >= info.Address + info.Size)
                    {
                        return KernelResult.OutOfMemory;
                    }

                    currentBase = info.Address + info.Size;

                    if (currentBase >= asInfo.Aslr.End)
                    {
                        return KernelResult.OutOfMemory;
                    }
                }

                currentEnd = currentBase + size;

                if (currentEnd < currentBase)
                {
                    return KernelResult.OutOfMemory;
                }
            }
        }

        public static Result GetAddressSpaceInfo(out AddressSpaceInfo info, int processHandle = CurrentProcessHandle)
        {
            info = new AddressSpaceInfo();

            Result result;

            result = KernelStatic.Syscall.GetInfo(InfoType.HeapRegionAddress, processHandle, 0, out info.Heap.Base);

            if (result != Result.Success)
            {
                return result;
            }

            result = KernelStatic.Syscall.GetInfo(InfoType.HeapRegionSize, processHandle, 0, out info.Heap.Size);

            if (result != Result.Success)
            {
                return result;
            }

            result = KernelStatic.Syscall.GetInfo(InfoType.AliasRegionAddress, processHandle, 0, out info.Alias.Base);

            if (result != Result.Success)
            {
                return result;
            }

            result = KernelStatic.Syscall.GetInfo(InfoType.AliasRegionSize, processHandle, 0, out info.Alias.Size);

            if (result != Result.Success)
            {
                return result;
            }

            result = KernelStatic.Syscall.GetInfo(InfoType.AslrRegionAddress, processHandle, 0, out info.Aslr.Base);

            if (result != Result.Success)
            {
                return result;
            }

            result = KernelStatic.Syscall.GetInfo(InfoType.AslrRegionSize, processHandle, 0, out info.Aslr.Size);

            if (result != Result.Success)
            {
                return result;
            }

            info.Heap.End = info.Heap.Base + info.Heap.Size;
            info.Alias.End = info.Alias.Base + info.Alias.Size;
            info.Aslr.End = info.Aslr.Base + info.Aslr.Size;

            return Result.Success;
        }
    }
}