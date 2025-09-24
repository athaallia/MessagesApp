namespace ChatClient
{
    public class ChatMessage
    {
        public string Type { get; set; } = "";     // "join" | "msg" | "pm" | "leave" | "sys"
        public string From { get; set; } = "";
        public string? To { get; set; }
        public string Text { get; set; } = "";
        public long Ts { get; set; }
    }
}
