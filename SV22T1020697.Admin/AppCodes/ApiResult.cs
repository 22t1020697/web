namespace SV22T1020697.Admin
/// <summary>
/// lớp biểu diênx kêyts quả khi gọi API
/// </summary>
{  public class ApiResult
    {
        public ApiResult(int code, string message) 
    {
        Code = code;
        Message = message;
    }
    /// <summary>
    /// lỗi hoặc ko thành công 
    /// </summary>
        public int Code { get; set; }
         public string Message { get; set; }
    
    }
}
