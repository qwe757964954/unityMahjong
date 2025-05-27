using UnityEngine;
using DG.Tweening;

namespace MahjongGame
{
    // Data model for a Mahjong tile
    public class MahjongTile
    {
        public MahjongType Type { get; set; }
        public string Suit { get; private set; }
        public int Number { get; private set; }
        public GameObject GameObject { get; private set; }
        public MahjongDisplay Display { get; private set; }

        public MahjongTile(MahjongType type, GameObject gameObject = null)
        {
            Type = type;
            SetGameObject(gameObject);

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
        }

        public void SetGameObject(GameObject gameObject)
        {
            GameObject = gameObject;
            Display = gameObject != null ? gameObject.GetComponent<MahjongDisplay>() : null;
            if (Display != null)
            {
                Display.SetType(Type);
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
                GameObject.transform.SetParent(parent, false);
            }
        }
    }
}