using Ninject.Syntax;

namespace Ninject.Extensions.Azure.Clients
{
    /// <summary>
    /// Extensions to IKernel or IResolutionRoot
    /// </summary>
    public static class KernelExtensions
    {
        /// <summary>
        /// Wraps a call to TryGet, with a named binding.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="kernel"></param>
        /// <param name="nameOfBinding"></param>
        /// <param name="client"></param>
        /// <returns></returns>
        internal static bool TryGetFromKernel<T>(this IResolutionRoot kernel, string nameOfBinding, out T client)
            where T : class
        {
            client = kernel.TryGet<T>(nameOfBinding);
            return client != null;
        }
    }
}