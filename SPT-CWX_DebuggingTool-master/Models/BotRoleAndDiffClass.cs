namespace CWX_DebuggingTool.Models
{
    public class BotRoleAndDiffClass
    {
        public BotRoleAndDiffClass(string role = "", string difficulty = "")
        {
            Role = role;
            Difficulty = difficulty;
        }

        public string Role { get; set; }
        public string Difficulty { get; set; }
    }
}