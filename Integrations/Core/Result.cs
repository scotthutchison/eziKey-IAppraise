namespace Integrations.Core
{
    public class Result<T> : Result
    {
        public T Value { get; set; }

        public Result(T value)
        {
            Value = value;
            Succeeded = true;
            ErrorList = new List<string>();
        }

        public Result(params string[] errors)
        {
            Succeeded = false;
            ErrorList = errors.ToList();
        }
    }

    public class Result
    {
        public List<string> ErrorList { get; set; }
        public bool Succeeded { get; set; }

        public Result()
        {
            Succeeded = true;
        }

        public Result(params string[] errors)
        {
            Succeeded = false;
            ErrorList = errors.ToList();
        }
    }
}
