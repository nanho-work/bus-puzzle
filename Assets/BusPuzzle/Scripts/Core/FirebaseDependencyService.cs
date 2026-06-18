using System.Threading.Tasks;
using Firebase;

namespace BusPuzzle
{
    internal static class FirebaseDependencyService
    {
        private static readonly object SyncRoot = new object();
        private static Task<DependencyStatus> dependencyTask;

        public static Task<DependencyStatus> CheckAndFixDependenciesAsync()
        {
            lock (SyncRoot)
            {
                if (dependencyTask == null || dependencyTask.IsCanceled || dependencyTask.IsFaulted)
                {
                    dependencyTask = FirebaseApp.CheckAndFixDependenciesAsync();
                }

                return dependencyTask;
            }
        }
    }
}
