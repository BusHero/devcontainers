namespace build.test;

public sealed record CommitMessage(string Type, string Message)
{
	public static CommitMessage New(string type) => new(type, Guid.NewGuid().ToString("N"));

	public override string ToString() => $"{Type}: {Message}";

	public static implicit operator string(CommitMessage commitMessage) => commitMessage.ToString();
}
