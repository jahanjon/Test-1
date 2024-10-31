namespace Common
{

    public class ResultDto<T>
    {
        public T Data { get; set; }

        public MessageCodes MessageCode { get; set; }

        public ResultDto()
        {
        }

        public ResultDto(T data)
        {
            Data = data;
            MessageCode = MessageCodes.Success;
        }

        public ResultDto(T data, MessageCodes messageCode)
        {
            Data = data;
            MessageCode = messageCode;
        }
    }

}