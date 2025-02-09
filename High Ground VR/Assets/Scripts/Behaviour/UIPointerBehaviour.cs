﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIPointerBehaviour : MonoBehaviour
{
    [SerializeField] private MenuManagement m_menuManagement; //Holds all logic for the buttons


    [SerializeField] private RectTransform m_cursor;
    [SerializeField] private Color m_defaultColour;
    [SerializeField] private Color m_highlightColour;

    [SerializeField,Space(10)] private RectTransform m_playButton;
    [SerializeField] private RectTransform m_handednessButton, m_infoButton, m_musicButton, m_effectsButton; // Main Menu Buttons
    [SerializeField] private RectTransform m_quitButton;
    [SerializeField] private RectTransform m_mainMenuButton;




    private Image m_cursorImage;
    private bool m_isUpdating;

    public bool isClicked;


    void Start()
    {
        m_cursorImage = m_cursor.gameObject.GetComponent<Image>();
    }
    void Update()
    {
        if(m_isUpdating == true)
        {
            m_cursor.gameObject.SetActive(true);
            if (rectOverlap(m_cursor, m_playButton) || rectOverlap(m_cursor, m_handednessButton) || rectOverlap(m_cursor, m_infoButton) || rectOverlap(m_cursor, m_musicButton) || rectOverlap(m_cursor, m_effectsButton) || rectOverlap(m_cursor, m_quitButton) || rectOverlap(m_cursor, m_mainMenuButton))
            {
                m_cursorImage.color = m_highlightColour;
            }
            else
            {
                m_cursorImage.color = m_defaultColour;
            }

            if (isClicked == true)
            {

                //Main Menu Controls
                if (rectOverlap(m_cursor, m_playButton) && m_playButton.gameObject.activeInHierarchy)
                {
                    GameManager.Instance.playGame();
                    AudioManager.Instance.PlaySound("buttonClick", AudioLists.UI, AudioMixers.UI, false, true, true, this.gameObject, 0.1f);
                    RumbleManager.Instance.lightVibration(InputManager.Instance.Handedness);
                }
                if (rectOverlap(m_cursor, m_handednessButton) && m_handednessButton.gameObject.activeInHierarchy)
                {
                    m_menuManagement.toggleHandedness();
                    AudioManager.Instance.PlaySound("buttonClick", AudioLists.UI, AudioMixers.UI, false, true, true, this.gameObject, 0.1f);
                    RumbleManager.Instance.lightVibration(InputManager.Instance.Handedness);
                }
                if (rectOverlap(m_cursor, m_infoButton) && m_infoButton.gameObject.activeInHierarchy)
                {
                    m_menuManagement.toggleInfoPanels();
                    AudioManager.Instance.PlaySound("buttonClick", AudioLists.UI, AudioMixers.UI, false, true, true, this.gameObject, 0.1f);
                    RumbleManager.Instance.lightVibration(InputManager.Instance.Handedness);
                }
                if (rectOverlap(m_cursor, m_musicButton) && m_musicButton.gameObject.activeInHierarchy)
                {
                    m_menuManagement.toggleMusicStatus();
                    AudioManager.Instance.PlaySound("buttonClick", AudioLists.UI, AudioMixers.UI, false, true, true, this.gameObject, 0.1f);
                    RumbleManager.Instance.lightVibration(InputManager.Instance.Handedness);
                }
                if (rectOverlap(m_cursor, m_effectsButton) && m_effectsButton.gameObject.activeInHierarchy)
                {
                    m_menuManagement.toggleEffectStatus();
                    AudioManager.Instance.PlaySound("buttonClick", AudioLists.UI, AudioMixers.UI, false, true, true, this.gameObject, 0.1f);
                    RumbleManager.Instance.lightVibration(InputManager.Instance.Handedness);
                }
                if (rectOverlap(m_cursor, m_quitButton) && m_quitButton.gameObject.activeInHierarchy)
                {
                    GameManager.Instance.exitGame();
                    RumbleManager.Instance.lightVibration(InputManager.Instance.Handedness);
                }
                //Game Menu Controls
                if (rectOverlap(m_cursor, m_mainMenuButton) && m_mainMenuButton.gameObject.activeInHierarchy)
                {
                    GameManager.Instance.GoToMainMenu();
                    RumbleManager.Instance.lightVibration(InputManager.Instance.Handedness);
                }
            }
        }
        else
        {
            m_cursor.gameObject.SetActive(false);
        }

        m_isUpdating = false;
    }



    /// <summary>
    /// Updates the position of the cursor object
    /// </summary>
    /// <param name="_pos"></param>
    public void updateCursorPos(Vector3 _pos)
    {
        m_isUpdating = true;
        m_cursor.position = _pos;
    }

    /// <summary>
    /// Returns whether or not Rect was overlapping. Used for buttons etc.
    /// </summary>
    /// <param name="rectTrans1"></param>
    /// <param name="rectTrans2"></param>
    /// <returns></returns>
    bool rectOverlap(RectTransform rectTrans1, RectTransform rectTrans2)
    {
        Rect _rect1 = new Rect(rectTrans1.localPosition.x - rectTrans1.rect.width/2 , rectTrans1.localPosition.y- rectTrans1.rect.height/2, rectTrans1.rect.width, rectTrans1.rect.height);
        Rect _rect2 = new Rect(rectTrans2.localPosition.x - rectTrans2.rect.width/2 , rectTrans2.localPosition.y - rectTrans2.rect.height/2, rectTrans2.rect.width, rectTrans2.rect.height);

        return _rect1.Overlaps(_rect2,true);
    }
}
