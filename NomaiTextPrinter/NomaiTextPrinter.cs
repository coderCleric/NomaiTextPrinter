using OWML.Common;
using OWML.ModHelper;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NomaiTextPrinter
{
    public class NomaiTextPrinter : ModBehaviour
    {
        private void Start()
        {
            // Starting here, you'll have access to OWML's mod helper.
            ModHelper.Console.WriteLine($"{nameof(NomaiTextPrinter)} is loaded!", MessageType.Success);
        }

        //When the player presses M, print info on the targeted text
        public void Update()
        {
            //Give a printout for the arc info for the collection the player is looking at
            if (Keyboard.current[Key.M].wasPressedThisFrame)
            {
                //Determine if the necessary assets are ready
                NomaiWallText text = null;
                bool validTarget = Locator.GetPlayerCamera() != null && 
                    (text = Locator.GetPlayerCamera().transform.Find("NomaiTranslatorProp").GetComponent<NomaiTranslatorProp>()._nomaiTextComponent as NomaiWallText) != null;

                //Only do things if there is a valid target
                if (validTarget)
                {
                    //Find the position of the text
                    Vector3 textPosition = text.transform.localPosition;

                    //Construct the start of the message
                    string msg = "{\"seed\": someseed, \"type\": \"wall\", \"xmlFile\": somexmlfile,"; //Basic header stuff
                    msg += "\"position\": {\"x\":" + textPosition.x + ", \"y\": " + textPosition.y + ", \"z\": " + textPosition.z + "},"; //Position
                    msg += "\"normal\": remembertomanuallyenternormal,"; //"Normal"
                    msg += "\"arcInfo\": ["; //Start of arcinfo

                    //Loop through each arc, getting the info for them
                    foreach (NomaiTextLine line in text.GetComponentsInChildren<NomaiTextLine>(true))
                    {
                        msg += "{";
                        Transform tf = line.transform;

                        //Mirror it if needed
                        if (tf.localScale.x == -1)
                            msg += "\"mirror\": true,";

                        //Determine & print the type
                        string matName = line.gameObject.GetComponent<MeshRenderer>().material.name;
                        if (matName.Contains("IP"))
                            msg += "\"type\": \"stranger\",";
                        else if(matName.Contains("NOM_TextChild"))
                            msg += "\"type\": \"child\",";
                        else
                            msg += "\"type\": \"adult\",";

                        //Print the position
                        msg += "\"position\": {\"x\": " + tf.localPosition.x + ",\"y\": " + tf.localPosition.y + "},";

                        //And the z rotation
                        msg += "\"zRotation\": " + tf.localRotation.eulerAngles.z;

                        msg += "}";
                    }
                    msg += "]}";

                    //Actually write the message
                    ModHelper.Console.WriteLine(msg);
                }
                else
                    ModHelper.Console.WriteLine("No valid text targeted!");
            }
        }
    }
}

