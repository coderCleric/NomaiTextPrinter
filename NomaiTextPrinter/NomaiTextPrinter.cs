using NewHorizons.External.Modules.TranslatorText;
using OWML.Common;
using OWML.ModHelper;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NomaiTextPrinter
{
    /**
     * TODO:
     * - Configurable key for the print
     * - Give them a seed if it's zero
     */
    public class NomaiTextPrinter : ModBehaviour
    {
        private NomaiTranslatorProp translator = null;
        private INewHorizons newHorizonsAPI = null;

        private void Start()
        {
            newHorizonsAPI = ModHelper.Interaction.TryGetModApi<INewHorizons>("xen.NewHorizons");

            ModHelper.Console.WriteLine($"{nameof(NomaiTextPrinter)} is loaded!", MessageType.Success);
        }

        //When the player presses M, print info on the targeted text
        public void Update()
        {
            //Give a printout for the arc info for the collection the player is looking at
            if (Keyboard.current[Key.M].wasPressedThisFrame)
            {
                NomaiText text = GetTranslatorText();

                //Only do things if there is a valid target
                if (text != null)
                {
                    //Try and get the text data from the json
                    AstroObject planet = text.GetComponentInParent<AstroObject>();
                    TranslatorTextInfo textInfo = GetTextInfo(planet.GetCustomName(), text._nomaiTextAsset.name);
                    if (textInfo == null)
                        return;

                    //Actually write the message
                    Transform NHObjectRoot = GetObjectRoot(text.transform, textInfo);
                    string msg = "{" + MakeNameText(NHObjectRoot.gameObject, textInfo);

                    //May need to use default values for position stuff
                    if (CanMove(textInfo.type))
                        msg += MakePositionTextFromInfo(textInfo);
                    else
                        msg += MakePositionText(NHObjectRoot);

                    msg += MakeInfoText(textInfo);
                    msg += MakeLocationText(text);

                    if (HasSpirals(textInfo.type))
                        msg += MakeArcText(text, textInfo) + "}";

                    ModHelper.Console.WriteLine(msg);
                }
            }
        }

        /**
         * Gets the root tf of the NH-placed object
         */
        private Transform GetObjectRoot(Transform textRoot, TranslatorTextInfo textInfo)
        {
            switch(textInfo.type)
            {
                case NomaiTextType.Recorder:
                case NomaiTextType.PreCrashRecorder:
                    return textRoot.parent;

                case NomaiTextType.CairnBrittleHollow:
                case NomaiTextType.CairnTimberHearth:
                case NomaiTextType.CairnEmberTwin:
                    return textRoot.parent.parent;

                case NomaiTextType.Trailmarker:
                    return textRoot.parent;

                default:
                    return textRoot;
            }
        }

        /**
         * Tells if a text type will have arrangeable spirals or not
         */
        private bool HasSpirals(NomaiTextType type)
        {
            return type == NomaiTextType.Scroll || type == NomaiTextType.Wall || type == NomaiTextType.Whiteboard;
        }

        /**
         * Tells if a text type can move
         */
        private bool CanMove(NomaiTextType type)
        {
            return type == NomaiTextType.Scroll || type == NomaiTextType.Whiteboard;
        }

        /**
         * Makes the json segment for the name
         */
        private string MakeNameText(GameObject rootGO, TranslatorTextInfo info)
        {
            if (CanMove(info.type) && info.rename != null)
                return $"\"rename\": \"{info.rename}\",";
            else if (CanMove(info.type))
                return "";
            return $"\"rename\": \"{rootGO.name}\",";
        }

        /**
         * Makes the json segment that describes the position & rotation of the text
         * Sets the:
         * - Position
         * - Rotation
         * - Parent path
         * - Is relative to parent
         */
        private string MakePositionText(Transform textTF)
        {
            string ret = "";

            //Start with the parent path
            Transform parent = textTF.parent;
            string parentPath = parent.name;
            while (parent.parent.gameObject.GetComponent<AstroObject>() == null)
            {
                parent = parent.parent;
                parentPath = parent.name + "/" + parentPath;
            }
            if(!parentPath.Equals("Sector"))
            {
                ret += $"\"parentPath\": \"{parentPath}\",";
                ret += $"\"isRelativeToParent\": true,";
            }

            //Then do the position
            Vector3 pos = textTF.localPosition;
            if (pos != Vector3.zero)
                ret += $"\"position\": {{\"x\": {pos.x}, \"y\": {pos.y}, \"z\": {pos.z}}},";

            //Finally, the rotation
            Vector3 rot = textTF.localRotation.eulerAngles;
            if (rot != Vector3.zero)
                ret += $"\"rotation\": {{\"x\": {rot.x}, \"y\": {rot.y}, \"z\": {rot.z}}},";

            return ret;
        }

        /**
         * Makes the json segment describing the position and rotation of the text, but using the text info from the json
         */
        private string MakePositionTextFromInfo(TranslatorTextInfo info)
        {
            string ret = "";

            //Start with the parent path
            string parentPath = info.parentPath;
            if (parentPath != null && !parentPath.Equals("Sector") && !parentPath.Equals(""))
            {
                ret += $"\"parentPath\": \"{parentPath}\",";
                ret += $"\"isRelativeToParent\": true,";
            }

            //Then do the position
            if (info.position != null)
            {
                Vector3 pos = info.position;
                if (pos != Vector3.zero)
                    ret += $"\"position\": {{\"x\": {pos.x}, \"y\": {pos.y}, \"z\": {pos.z}}},";
            }

            //Finally, the rotation
            if (info.rotation != null)
            {
                Vector3 rot = info.rotation;
                if (rot != Vector3.zero)
                    ret += $"\"rotation\": {{\"x\": {rot.x}, \"y\": {rot.y}, \"z\": {rot.z}}},";
            }

            return ret;
        }

        /**
         * Makes the json segment that uses the text info from the json
         * Sets the:
         * - Seed
         * - Type
         * - xml file
         */
        private string MakeInfoText(TranslatorTextInfo textInfo)
        {
            string ret = "";

            //Start with the type
            string typeStr = "wall";
            switch (textInfo.type)
            {
                case NomaiTextType.Scroll:
                    typeStr = "scroll";
                    break;

                case NomaiTextType.Whiteboard:
                    typeStr = "whiteboard";
                    break;

                case NomaiTextType.Computer:
                    typeStr = "computer";
                    break;

                case NomaiTextType.PreCrashComputer:
                    typeStr = "preCrashComputer";
                    break;

                case NomaiTextType.Recorder:
                    typeStr = "recorder";
                    break;

                case NomaiTextType.PreCrashRecorder:
                    typeStr = "preCrashRecorder";
                    break;

                case NomaiTextType.CairnBrittleHollow:
                    typeStr = "cairnBH";
                    break;

                case NomaiTextType.CairnTimberHearth:
                    typeStr = "cairnTH";
                    break;

                case NomaiTextType.CairnEmberTwin:
                    typeStr = "cairnCT";
                    break;

                case NomaiTextType.Trailmarker:
                    typeStr = "trailmarker";
                    break;
            }
            ret += $"\"type\": \"{typeStr}\",";

            //Then, get the xml file
            ret += $"\"xmlFile\": \"{textInfo.xmlFile}\",";

            //Finally, get the seed
            if (HasSpirals(textInfo.type) && textInfo.seed != 0)
                ret += $"\"seed\": {textInfo.seed},";

            return ret;
        }

        /**
         * Makes the json segment for the location
         */
        private string MakeLocationText(NomaiText text)
        {
            switch(text._location)
            {
                case NomaiText.Location.A:
                    return "\"location\": \"a\",";

                case NomaiText.Location.B:
                    return "\"location\": \"b\",";

                default:
                    return "";
            }
        }

        /**
         * Makes the json segment for the arc info
         */
        private string MakeArcText(NomaiText text, TranslatorTextInfo info)
        {
            NomaiWallText wallText = text as NomaiWallText;

            //Make the header text for the arc section
            string ret = "\"arcInfo\":[";

            //Iterate through all of the arcs, finding the info for each
            bool isFirst = true;
            for(int i = 0; i < wallText._textLines.Length; i++)
            {
                NomaiTextLine line = wallText._textLines[i];

                //Comma before every one but the first
                if (!isFirst)
                    ret += ",";
                isFirst = false;

                //First, use the material to determine the type
                string matName = line.GetComponent<Renderer>().sharedMaterial.name;
                string typeName = "unknown";
                if (matName.Contains("Effects_NOM_Text_mat"))
                    typeName = "adult";
                else if (matName.Contains("Effects_NOM_TextChild_mat"))
                {
                    typeName = info.arcInfo[i].type.ToString().ToLower();
                }
                else if (matName.Contains("Effects_IP_Text_mat"))
                    typeName = "stranger";
                ret += $"{{\"type\": \"{typeName}\",";

                //Then, determine if it's been flipped
                if (line.transform.localScale.x < 0)
                    ret += "\"mirror\": true,";

                //Find the position
                Vector3 pos = line.transform.localPosition;
                ret += $"\"position\": {{\"x\": {pos.x}, \"y\": {pos.y}}},";

                //And finally the rotation
                ret += $"\"zRotation\": {line.transform.localRotation.eulerAngles.z}}}";
            }

            //Make the end of the arc section
            ret += "]";

            return ret;
        }

        /**
         * Try to get the translator text. Returns null if it can't be found for some reason
         */
        private NomaiText GetTranslatorText()
        {
            //May or may not need to find the translator
            if (translator == null)
            {
                //If some part of the chain is broken, return null
                if (Locator.GetActiveCamera() == null || Locator.GetPlayerCamera().transform.Find("NomaiTranslatorProp") == null)
                {
                    ModHelper.Console.WriteLine("Couldn't find translator prop! Are you still on the main menu?", MessageType.Error);
                    return null;
                }

                translator = Locator.GetPlayerCamera().transform.Find("NomaiTranslatorProp").GetComponent<NomaiTranslatorProp>();
            }

            //If there's no text, return null
            if (translator._nomaiTextComponent == null)
            {
                ModHelper.Console.WriteLine("Couldn't find any targeted text!", MessageType.Error);
                return null;
            }

            //Otherwise, return the translator text
            return translator._nomaiTextComponent;
        }

        /**
         * Query a body to find text data for a specific text
         */
        private TranslatorTextInfo GetTextInfo(string bodyName, string textName)
        {
            //Get the whole list of texts from the body
            TranslatorTextInfo[] texts = newHorizonsAPI.QueryBody<TranslatorTextInfo[]>(bodyName, "$.Props.translatorText");

            //Look for one with a matching name
            TranslatorTextInfo info = null;
            int count = 0;
            foreach (TranslatorTextInfo text in texts)
            {
                string xmlName = Path.GetFileNameWithoutExtension(text.xmlFile);
                if (xmlName.Equals(textName))
                {
                    info = text;
                    count++;
                }
            }

            //Error if we found no good texts
            if (info == null)
                ModHelper.Console.WriteLine("Could not find text with matching name!", MessageType.Error);

            if(count > 1)
                ModHelper.Console.WriteLine("Multiple texts with same xml name found.", MessageType.Warning);

            return info;
        }
    }
}

