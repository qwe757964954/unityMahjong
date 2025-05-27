// ========== Enhanced MahjongTile Class ==========
using UnityEngine;

namespace MahjongGame
{
    public class MahjongTile
    {
        public MahjongType Type { get; private set; }
        public string Suit { get; private set; }
        public int Number { get; private set; }
        public GameObject GameObject { get; set; }
        public MahjongDisplay Display { get; private set; }

        public MahjongTile(MahjongType type, GameObject gameObject)
        {
            Type = type;
            GameObject = gameObject;
            Display = gameObject.GetComponent<MahjongDisplay>();
            
            // Parse suit and number
            if (type >= MahjongType.Dot1 && type <= MahjongType.Dot9)
            {
                Suit = "Dot";
                Number = (int)type - (int)MahjongType.Dot1 + 1;
            }
            else if (type >= MahjongType.Bamboo1 && type <= MahjongType.Bamboo9)
            {
                Suit = "Bamboo";
                Number = (int)type - (int)MahjongType.Bamboo1 + 1;
            }
            else if (type >= MahjongType.Character1 && type <= MahjongType.Character9)
            {
                Suit = "Character";
                Number = (int)type - (int)MahjongType.Character1 + 1;
            }
            else
            {
                Suit = "Honor";
                Number = 0;
            }
            
            // Set the display type
            if (Display != null)
            {
                Display.SetType(type);
            }
        }

        public void SetPosition(Vector3 position)
        {
            if (GameObject != null)
            {
                GameObject.transform.position = position;
            }
        }

        public void SetLocalPosition(Vector3 localPosition)
        {
            if (GameObject != null)
            {
                GameObject.transform.localPosition = localPosition;
            }
        }

        public void SetRotation(Quaternion rotation)
        {
            if (GameObject != null)
            {
                GameObject.transform.rotation = rotation;
            }
        }

        public void SetLocalRotation(Quaternion rotation)
        {
            if (GameObject != null)
            {
                GameObject.transform.localRotation = rotation;
            }
        }

        public void SetParent(Transform parent)
        {
            if (GameObject != null)
            {
                GameObject.transform.SetParent(parent);
            }
        }
    }
}
