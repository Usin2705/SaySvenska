using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class TextUtils
{ 
    /// <summary>
    /// Formats the given transcript by applying color tags based on the corresponding scores in the scoreList.
    /// </summary>
    /// <param name="transcript">The text to be formatted.</param>
    /// <param name="scoreList">A list of scores corresponding to each character in the transcript.</param>
    /// <returns>A formatted string with color tags applied to each character based on the scores.</returns>
    /// <remarks>
    /// The font used in the ResultText GameObject is already set as BOLD, so there is no need to add BOLD tags.
    /// </remarks>
    /// <exception cref="System.ArgumentException">Thrown when the length of the transcript does not match the count of the scoreList.</exception>
    /// <example>
    /// <code>
    /// string transcript = "hello";
    /// List<float> scores = new List<float> { 0.9f, 0.8f, 0.5f, 0.3f, 0.7f };
    /// string result = TextUtils.FormatTextResult(transcript, scores);
    /// // result might be: "<color=goodColor>h</color><color=goodColor>e</color><color=avgColor>l</color><color=badColor>l</color><color=avgColor>o</color>"
    /// </code>
    /// </example>
    /// <remarks>
    /// This method is static because it does not depend on any instance-specific data. 
    /// It operates solely on the input parameters provided, making it a utility function that can be called without instantiating the class.
    /// </remarks>
    public static string FormatTextResult(string transcript, List<float> scoreList) 
	{
		// Make sure that stranscript length match with scoreList Length
		if (transcript.Length != scoreList.Count) {	
            Debug.LogError("transcript and score didn't match: " + transcript + " vs " + scoreList.Count + " ");
			return "";
		}

        string textResult = "";

		for (int i = 0; i < scoreList.Count; i++) 
        {
            string phoneColor = Const.GOOD_COLOR;

            if (scoreList[i] < Const.BAD_SCORE)  phoneColor = Const.BAD_COLOR; 
            else if (scoreList[i] < Const.AVG_SCORE) phoneColor = Const.AVG_COLOR;
            
            textResult += "<color=" + phoneColor + ">" + transcript[i].ToString() + "</color>";

        }
        
		return textResult;
	}

        /// <summary>
    /// Sanitizes the provided text to ensure it conforms to specific requirements.
    /// Santinize Text should be done at the unity level, since we will compare the transcript with OPS operations in server
    /// </summary>
    /// <remarks>
    /// <para>The sanitization process involves:</para>
    /// <list type="bullet">
    /// <item><description>Replacing newline characters with spaces.</description></item>
    /// <item><description>Replacing hyphens "-" with spaces.</description></item>
    /// <item><description>Removing trailing white spaces.</description></item>
    /// <item><description>Replacing multiple spaces with a single space.</description></item>
    /// <item><description>Replacing Z or z with ts.</description></item>
    /// <item><description>Removing numbers.</description></item>
    /// <item><description>Removing symbols except for hyphens (which are removed earlier).</description></item>
    /// </list>
    /// <para>This sanitization process is essential when comparing the transcript with OPS operations.</para>
    /// </remarks>
    /// <param name="text">The input text string that needs to be sanitized.</param>
    /// <returns>Returns the sanitized version of the input text.</returns>
    public static string SantinizeText(string text)
    {
        text = text.Replace("\n", " ");
        text = text.Replace("-", " "); // Replace - with space, should be done before removing extra space

        text = text.Trim(); // Remove trailing white space
        text = Regex.Replace(text, "  +", " "); // Replace extra spaces with just 1 space        

        text = Regex.Replace(text, "[0-9]", ""); //Remove numbers        
        text = Regex.Replace(text, "[!$%^&*()_+|~=`{}\\[\\]:\";'<>?,.\\/@]", ""); //Remove symbols (not -) 
                
        return text;
    }
}