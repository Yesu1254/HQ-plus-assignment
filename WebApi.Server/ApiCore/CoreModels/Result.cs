using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiCore.CoreModels
{
    public class Result<T> : Result
    {
        public T Value;

        public Result() : base(false, "")
        {

        }

        public Result(T obj) : base(true, "")
        {
            Value = obj;
        }


        public Result(bool success, string errorMessage = "") : base(success, errorMessage)
        {
        }

        public Result<T> Error(string message)
        {
            this.ErrorMessage = message;
            this.Successful = false;
            return this;
        }

        public Result<T> Ok()
        {
            this.ErrorMessage = "";
            this.Successful = true;
            return this;
        }

        public Result<T> Ok(T obj)
        {
            this.ErrorMessage = "";
            this.Successful = true;
            this.Value = obj;
            return this;
        }

    }

    public class Result
    {
        public bool Successful { get; set; }
        [JsonProperty("ex")] public string ErrorMessage { get; set; } = "";
        public object Data { get; set; }


        public Result(bool success, string errorMessage = "")
        {
            Successful = success;
            ErrorMessage = errorMessage;
        }

        public static Result Success(object data)
        {
            return new Result(true) { Data = data };
        }

        public static Result Fail(Exception ex)
        {
            return new Result(false, ex == null ? "Unknown" : ex.Message);
        }

        public static Result Fail(string message)
        {
            return new Result(false, message);
        }

        public static Result Fail<T>(string message)
        {
            return new Result<T>(false, message);
        }

        public static Result Success()
        {
            return new Result(true);
        }

        public static Result<T> Success<T>(T obj)
        {
            return new Result<T>(obj);
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(Result me, bool that)
        {
            return me != null && me.Successful == that;
        }

        public static bool operator !=(Result me, bool that)
        {
            return !me.Successful == that;
        }

        public static implicit operator bool(Result v) { return v.Successful; }
    }
}
