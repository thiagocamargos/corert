// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

// 

//
// Abstract derivations of SafeHandle designed to provide the common
// functionality supporting Win32 handles. More specifically, they describe how
// an invalid handle looks (for instance, some handles use -1 as an invalid
// handle value, others use 0).
//
// Further derivations of these classes can specialise this even further (e.g.
// file or registry handles).
// 
//

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Microsoft.Win32.SafeHandles
{
    // Class of safe handle which uses 0 or -1 as an invalid handle.
    public abstract class SafeHandleZeroOrMinusOneIsInvalid : SafeHandle
    {
        protected SafeHandleZeroOrMinusOneIsInvalid(bool ownsHandle) : base(IntPtr.Zero, ownsHandle)
        {
        }

        protected SafeHandleZeroOrMinusOneIsInvalid(IntPtr handle, bool ownsHandle) : base(handle, ownsHandle)
        {
        }

        public override bool IsInvalid
        {
            get
            { return DangerousGetHandle() == IntPtr.Zero || DangerousGetHandle() == new IntPtr(-1); }
        }
    }

    // Class of safe handle which uses only -1 as an invalid handle.
    public abstract class SafeHandleMinusOneIsInvalid : SafeHandle
    {
        protected SafeHandleMinusOneIsInvalid(bool ownsHandle) : base(new IntPtr(-1), ownsHandle)
        {
        }

        public override bool IsInvalid
        {
            get
            { return DangerousGetHandle() == new IntPtr(-1); }
        }
    }
    //    // Class of critical handle which uses 0 or -1 as an invalid handle.
    //    public abstract class CriticalHandleZeroOrMinusOneIsInvalid : CriticalHandle
    //    {
    //        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
    //        protected CriticalHandleZeroOrMinusOneIsInvalid() : base(IntPtr.Zero) 
    //        {
    //        }

    //        public override bool IsInvalid {
    //            get { return handle.IsNull() || handle == new IntPtr(-1); }
    //        }
    //    }

    //    // Class of critical handle which uses only -1 as an invalid handle.
    //#if !FEATURE_CORECLR
    //    [SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode=true)]
    //#endif
    //    public abstract class CriticalHandleMinusOneIsInvalid : CriticalHandle
    //    {
    //        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
    //        protected CriticalHandleMinusOneIsInvalid() : base(new IntPtr(-1)) 
    //        {
    //        }

    //        public override bool IsInvalid {
    //            get { return handle == new IntPtr(-1); }
    //        }
    //    }
}
