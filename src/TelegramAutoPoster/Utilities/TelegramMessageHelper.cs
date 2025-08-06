namespace TelegramAutoPoster.Utilities
{
	public static class TelegramMessageHelper
	{
		//private const int MaxCaptionLength = 1024;
		//private const int MaxMessageLength = 4096;

		private const int MaxCaptionLength = 700;
		private const int MaxMessageLength = 3500;

		public static (string FirstPart, List<string> RemainingParts) SplitFormattedText(string markdownText)
		{
			var remainingParts = new List<string>();
			string firstPart = GetFirstPartWithQuotes(markdownText, MaxCaptionLength);
			string remainingText = markdownText.Substring(firstPart.Length).Trim();

			// Split remaining text into chunks (respecting quotes + sentences)
			while (remainingText.Length > 0)
			{
				int chunkSize = Math.Min(MaxMessageLength, remainingText.Length);
				string chunk = GetNextChunkWithQuotes(remainingText, chunkSize);
				remainingParts.Add(chunk);
				remainingText = remainingText.Substring(chunk.Length).Trim();
			}

			return (firstPart, remainingParts);
		}

		private static string GetFirstPartWithQuotes(string text, int maxLength)
		{
			if (text.Length <= maxLength)
				return text;

			// Find the nearest sentence end or quote boundary before maxLength
			int splitPos = FindSafeSplitPosition(text, maxLength);
			return text.Substring(0, splitPos).Trim();
		}

		private static string GetNextChunkWithQuotes(string text, int maxLength)
		{
			if (text.Length <= maxLength)
				return text;

			int splitPos = FindSafeSplitPosition(text, maxLength);
			return text.Substring(0, splitPos).Trim();
		}

		private static int FindSafeSplitPosition(string text, int maxLength)
		{
			int splitPos = Math.Min(maxLength, text.Length);

			// Prefer splitting at:
			// 1. End of quotes (“...”)
			// 2. Sentence boundaries (. ! ? followed by space/newline)
			// 3. Paragraph breaks (\n\n)
			for (int i = splitPos - 1; i >= 0; i--)
			{
				if (i < text.Length - 1 && text[i] == '”' && (text[i + 1] == ' ' || text[i + 1] == '\n'))
				{
					return i + 1; // Split after closing quote
				}
				if ((text[i] == '.' || text[i] == '!' || text[i] == '?') &&
					(i == text.Length - 1 || text[i + 1] == ' ' || text[i + 1] == '\n'))
				{
					return i + 1; // Split after sentence end
				}
				if (i > 0 && text[i] == '\n' && text[i - 1] == '\n')
				{
					return i + 1; // Split after paragraph break
				}
			}

			// Fallback: Split at maxLength (hard cut if no breaks found)
			return splitPos;
		}

		private static string GetFirstPart(string text)
		{
			if (text.Length <= MaxCaptionLength)
				return text;

			// Find last sentence end before MaxCaptionLength
			int cutPosition = MaxCaptionLength;
			for (int i = MaxCaptionLength; i >= 0; i--)
			{
				if (i < text.Length && IsNaturalBreak(text, i))
				{
					cutPosition = i;
					break;
				}
			}

			// Fallback: Hard cut if no breaks found
			if (cutPosition == MaxCaptionLength)
			{
				cutPosition = Math.Min(MaxCaptionLength, text.Length);
			}

			return text.Substring(0, cutPosition).Trim();
		}

		private static bool IsNaturalBreak(string text, int position)
		{
			// Check for sentence/paragraph breaks
			if (position >= text.Length) return true;

			return position > 0 && (
				text[position] == '\n' ||                          // Line break
				text[position] == '.' && (position + 1 >= text.Length || char.IsWhiteSpace(text[position + 1])) ||  // Sentence end
				text[position] == '!' ||
				text[position] == '?'
			);
		}

		private static List<string> SplitRemainingText(string text)
		{
			var parts = new List<string>();
			int currentPos = 0;

			while (currentPos < text.Length)
			{
				int nextChunkSize = Math.Min(MaxMessageLength, text.Length - currentPos);
				int endPos = currentPos + nextChunkSize;

				// Try to extend to next natural break
				if (endPos < text.Length)
				{
					for (int i = endPos; i < Math.Min(endPos + 200, text.Length); i++)
					{
						if (IsNaturalBreak(text, i))
						{
							endPos = i;
							break;
						}
					}
				}

				parts.Add(text.Substring(currentPos, endPos - currentPos).Trim());
				currentPos = endPos;
			}

			return parts;
		}
	}
}