using EFT.InventoryLogic;
using System.Collections.Generic;
using System.Linq;

namespace ShowMeTheStats
{
    public static class Utils
    {

        public static string AlignTextToWidth(string text)
        {
            int spaces = 20;

            int currentLength = text.Length;

            int newLength = spaces - currentLength;

            if (currentLength < spaces)
            {
                for (int i = 0; i < newLength; i++)
                {
                    text += " ";
                }
            }

            return text;
        }

        public static string[] getDigitsFromStringValues(string attributeStringValue)
        {
            string extractedDigits = "";
            string extractedOperator = "";
            string extractedType = "";

            foreach (char c in attributeStringValue)
            {
                if (char.IsDigit(c) || c == '.')
                {
                    extractedDigits += c.ToString();
                }
                else if (c == '-' || c == '+')
                {
                    extractedOperator = c.ToString();
                }
                else if (c == '%' || c == 'M' || c == 'O' || c == 'A') // lazy ahh MOA extraction
                {
                    extractedType += c.ToString();
                }
            }

            if (extractedType == "MOA")
            {
                extractedType = " " + extractedType;
            }

            string[] final = { extractedOperator, extractedDigits, extractedType };

            return final;
        }

        public static string SubstractStringValue(string slottedAttributeStringValue, string replacingAttributeStringValue)
        {
            //[0] = operator
            //[1] = float
            //[2] = "%" string

            string[] slottingAttributeExtracted = getDigitsFromStringValues(slottedAttributeStringValue);
            string[] replacingAttributeExtracted = getDigitsFromStringValues(replacingAttributeStringValue);

            string subtracted = (float.Parse(replacingAttributeExtracted[0] + replacingAttributeExtracted[1]) - float.Parse(slottingAttributeExtracted[0] + slottingAttributeExtracted[1])).ToString(); //"F1" 

            return subtracted + replacingAttributeExtracted[2];
        }

        public static string GetValueColor(float numBase, bool LessIsGood, EItemAttributeLabelVariations labelVariation, bool reversed)
        {
            string blueColor = "#54c1ff";
            string redColor = "#c40000";

            string textColor = "";
            if (labelVariation == EItemAttributeLabelVariations.Colored)
            {
                if (numBase < 0f)
                {
                    if (LessIsGood)
                    {
                        textColor = blueColor;
                    }
                    else if (!LessIsGood)
                    {
                        textColor = redColor;
                    }
                }
                else if (numBase > 0f)
                {
                    if (LessIsGood)
                    {
                        textColor = redColor;
                    }
                    else if (!LessIsGood)
                    {
                        textColor = blueColor;
                    }
                }

                if (reversed)
                {
                    if (textColor == blueColor)
                    {
                        textColor = redColor;
                    }
                    else if (textColor == redColor)
                    {
                        textColor = blueColor;
                    }
                }
            }
            else
            {
                textColor = "#ffffff"; // white
            }

            return textColor;
        }

        public static string ReverseOperator(string stringValue)
        {
            if (stringValue.Contains("-"))
            {
                stringValue = stringValue.Replace("-", "+");
            }
            else if (stringValue.Contains("+"))
            {
                stringValue = stringValue.Replace("+", "-");
            }

            return stringValue;
        }

        public static string SpaghettiLastStringValueOperatorCheck(string attributeStringValue, float attributeBase)
        {
            if (attributeStringValue.Trim().ToUpper().Contains("LOUDNESS") || attributeStringValue.Trim().ToUpper().Contains("MOA") || attributeStringValue.Trim().ToUpper().Contains("MAX COUNT"))
            {
                if (attributeStringValue.Contains("+"))
                {
                    attributeStringValue = attributeStringValue.Replace("+", "");
                }
                return attributeStringValue;
            }
            else if (!attributeStringValue.Contains("+") && !attributeStringValue.Contains("-"))
            {
                return "+" + attributeStringValue;
            }


            return attributeStringValue;
        }

        public static string AddOperatorToStringValue(string attributeStringValue, float attributeBase, bool reversed)
        {
            if (attributeBase < 0f)
            {
                if (!attributeStringValue.Contains("-"))
                {
                    if (!reversed)
                    {
                        attributeStringValue = "-" + attributeStringValue;
                    }
                    else if (reversed)
                    {
                        attributeStringValue = "+" + attributeStringValue;
                    }
                }
            }
            else
            {
                if (!reversed)
                {
                    attributeStringValue = "+" + attributeStringValue;

                }
                else if (reversed)
                {
                    attributeStringValue = "-" + attributeStringValue;
                }
            }

            return attributeStringValue;
        }

        public static List<ItemAttribute> GetAllAttributesNotInBlacklist(List<ItemAttribute> attributes)
        {
            List<ItemAttribute> attributesResult = new List<ItemAttribute>();
            foreach (var attribute in attributes)
            {
                if (!Globals.statBlacklist.Any(x => x == attribute.Id.ToString()))
                {
                    attributesResult.Add(attribute);
                }
            }
            return attributesResult;
        }
    }
}
