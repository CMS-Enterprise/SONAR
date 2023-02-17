namespace Cms.BatCave.Sonar.Threading.Tasks;

/// <summary>
///   This is the missing interface for adding <c>await</c> keyword support to your C# class.
/// </summary>
/// <typeparam name="T">
///   The type of the object yielded when an instance of this interface is awaited.
/// </typeparam>
public interface IAwaitable<out T> {
  IAwaiter<T> GetAwaiter();
}
