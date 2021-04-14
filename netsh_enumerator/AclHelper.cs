using System;
using System.Runtime.InteropServices;

namespace netsh_enumerator
{
    //Code from https://github.com/Cacowned/mayhem/blob/060d575f0fdeb02728218e285a25f7c75d528826/Modules/PhoneModules/ACLHelper.cs
    public static class AclHelper
    {
        internal enum HTTP_SERVICE_CONFIG_QUERY_TYPE
        {
            HttpServiceConfigQueryExact,
            HttpServiceConfigQueryNext,
            HttpServiceConfigQueryMax
        }

        internal struct HTTP_SERVICE_CONFIG_URLACL_KEY
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pUrlPrefix;
        }

        internal enum HTTP_SERVICE_CONFIG_ID
        {
            HttpServiceConfigIPListenList,
            HttpServiceConfigSSLCertInfo,
            HttpServiceConfigUrlAclInfo,
            HttpServiceConfigTimeout,
            HttpServiceConfigMax
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct HTTP_SERVICE_CONFIG_URLACL_QUERY
        {
            public HTTP_SERVICE_CONFIG_QUERY_TYPE QueryDesc;
            public HTTP_SERVICE_CONFIG_URLACL_KEY KeyDesc;
            public uint dwToken;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct HTTP_SERVICE_CONFIG_URLACL_SET
        {
            public HTTP_SERVICE_CONFIG_URLACL_KEY KeyDesc;
            public HTTP_SERVICE_CONFIG_URLACL_PARAM ParamDesc;
        }

        internal struct HTTP_SERVICE_CONFIG_URLACL_PARAM
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pStringSecurityDescriptor;
        }

        internal struct HTTPAPI_VERSION
        {
            public ushort HttpApiMajorVersion;
            public ushort HttpApiMinorVersion;
        }

        [DllImport("Httpapi.dll")]
        internal static extern uint HttpQueryServiceConfiguration(IntPtr ServiceHandle, HTTP_SERVICE_CONFIG_ID ConfigId, IntPtr pInputConfigInfo, uint InputConfigLength, IntPtr pOutputConfigInfo, uint OutputConfigInfoLength, ref uint pReturnLength, IntPtr pOverlapped);

        [DllImport("Httpapi.dll")]
        internal static extern uint HttpInitialize(HTTPAPI_VERSION Version, uint Flags, IntPtr pReserved);

        internal const uint ERROR_NO_MORE_ITEMS = 259;
        internal const uint ERROR_INSUFFICIENT_BUFFER = 122;
        internal const uint NO_ERROR = 0;
        internal const uint HTTP_INITIALIZE_CONFIG = 2;

        static AclHelper()
        {
            HTTPAPI_VERSION version = new HTTPAPI_VERSION();
            version.HttpApiMajorVersion = 1;
            version.HttpApiMinorVersion = 0;

            HttpInitialize(version, HTTP_INITIALIZE_CONFIG, IntPtr.Zero);
        }

        public static bool FindUrlPrefix(string urlPrefix)
        {
            HTTP_SERVICE_CONFIG_URLACL_QUERY query = new HTTP_SERVICE_CONFIG_URLACL_QUERY();
            query.QueryDesc = HTTP_SERVICE_CONFIG_QUERY_TYPE.HttpServiceConfigQueryNext;

            IntPtr pQuery = Marshal.AllocHGlobal(Marshal.SizeOf(query));

            try
            {
                uint retval = NO_ERROR;
                for (query.dwToken = 0; ; query.dwToken++)
                {
                    Marshal.StructureToPtr(query, pQuery, false);

                    try
                    {
                        uint returnSize = 0;

                        // Get Size
                        retval = HttpQueryServiceConfiguration(IntPtr.Zero, HTTP_SERVICE_CONFIG_ID.HttpServiceConfigUrlAclInfo, pQuery, (uint)Marshal.SizeOf(query), IntPtr.Zero, 0, ref returnSize, IntPtr.Zero);

                        if (retval == ERROR_NO_MORE_ITEMS)
                        {
                            break;
                        }

                        if (retval != ERROR_INSUFFICIENT_BUFFER)
                        {
                            throw new Exception("HttpQueryServiceConfiguration returned unexpected error code.");
                        }

                        IntPtr pConfig = Marshal.AllocHGlobal((IntPtr)returnSize);

                        string foundPrefix;
                        try
                        {
                            retval = HttpQueryServiceConfiguration(IntPtr.Zero, HTTP_SERVICE_CONFIG_ID.HttpServiceConfigUrlAclInfo, pQuery, (uint)Marshal.SizeOf(query), pConfig, returnSize, ref returnSize, IntPtr.Zero);
                            HTTP_SERVICE_CONFIG_URLACL_SET config = (HTTP_SERVICE_CONFIG_URLACL_SET)Marshal.PtrToStructure(pConfig, typeof(HTTP_SERVICE_CONFIG_URLACL_SET));
                            foundPrefix = config.KeyDesc.pUrlPrefix;
                            Console.WriteLine(foundPrefix);
                        }
                        finally
                        {
                            Marshal.FreeHGlobal(pConfig);
                        }

                        if (foundPrefix == urlPrefix)
                        {
                            return true;
                        }
                    }
                    finally
                    {
                        Marshal.DestroyStructure(pQuery, typeof(HTTP_SERVICE_CONFIG_URLACL_QUERY));
                    }
                }

                if (retval != ERROR_NO_MORE_ITEMS)
                {
                    throw new Exception("HttpQueryServiceConfiguration returned unexpected error code.");
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pQuery);
            }

            return false;
        }
    }
}

