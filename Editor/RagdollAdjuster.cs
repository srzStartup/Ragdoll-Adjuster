using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEditor;

public class RagdollAdjuster : EditorWindow
{
    // texts
    private static string windowNameText = "Ragdoll Adjuster";
    private string descriptionText = "Drag the ragdoll parent transform and click apply to adjust children's masses.";
    private string rootText = "Ragdoll Parent";
    private string currentTotalMassInfoText = "Current total mass";
    private string newMassText = "New Mass";
    private string includeChildrenText = "Include children?";

    private string checkButtonText = "Check";
    private string applyButtonText = "Apply";
    private string clearButtonText = "Clear";

    private Transform _ragdollParent;
    private List<Collider> _ragdollParts;
    private float _newMass;

    private Dictionary<Transform, float> _massRatios = new Dictionary<Transform, float>();
    private float _totalMass;
    private bool _includeChildren = false;

    private bool _isChecked = false;
    private Vector2 _scrollView = Vector2.zero;

    public Transform[] ignoreParts;

    SerializedObject so;
    SerializedProperty ignorePartsProperty;

    [MenuItem("Window/Ragdoll Adjuster")]
    public static void ShowWindow()
    {
        GetWindow<RagdollAdjuster>(windowNameText);
    }

    void OnEnable()
    {
        so = new SerializedObject(this);
        ignorePartsProperty = so.FindProperty("ignoreParts");
    }

    void OnDisable()
    {
        ResetVariables();
    }

    void OnGUI()
    {
        Title();

        _ragdollParent = EditorGUILayout.ObjectField(rootText, _ragdollParent, typeof(Transform), true) as Transform;

        EditorGUILayout.PropertyField(ignorePartsProperty, true);
        so.ApplyModifiedProperties();

        _includeChildren = EditorGUILayout.Toggle(includeChildrenText + " ", _includeChildren);

        EditorGUILayout.Space(5);

        _newMass = EditorGUILayout.FloatField(newMassText + ": ", _newMass);

        EditorGUILayout.Space(25);

        Information();

        if (GUILayout.Button(checkButtonText))
        {
            OnCheckClicked();
        }

        if (GUILayout.Button(applyButtonText))
        {
            OnApplyClicked();
        }

        if (GUILayout.Button(clearButtonText))
        {
            OnClearClicked();
        }
    }

    private void Title()
    {
        EditorGUILayout.Space(10);

        GUILayout.Label(descriptionText);

        EditorGUILayout.Space();
    }

    private void Information()
    {
        string totalMassInfoText = _ragdollParent != null ? _totalMass.ToString() : "none";

        GUILayout.Box(currentTotalMassInfoText + ": " + totalMassInfoText, EditorStyles.helpBox);

        if (_isChecked)
        {
            _scrollView = GUILayout.BeginScrollView(_scrollView, false, true);

            foreach (Collider c in _ragdollParts)
            {

                string transformNameText = c.transform.name;
                string currentMassText = "\nCurrent Mass:\t" + c.attachedRigidbody.mass;
                string currentRatioText = "\nCurrent Ratio:\t" + _massRatios[c.transform];

                GUILayout.Label(transformNameText, EditorStyles.boldLabel);
                GUILayout.Label(currentMassText + "\n" + currentRatioText + "\n");
                GUILayout.Label("----------");
            }

            GUILayout.EndScrollView();
        }

        EditorGUILayout.Space(10);
    }

    private void OnCheckClicked()
    {
        if (_isChecked) return;

        if (_ragdollParent != null)
        {
            _ragdollParts = _ragdollParent.GetComponentsInChildren<Transform>()
                .ToList()
                .FindAll(ragdollPart =>
                {
                    if (ignoreParts != null)
                    {
                        List<Transform> ignoredAll = new List<Transform>();

                        if (_includeChildren)
                        {
                            foreach (Transform ignorePart in ignoreParts)
                            {
                                foreach (Transform child in ignorePart.GetComponentsInChildren<Transform>())
                                {
                                    ignoredAll.Add(child);
                                }
                            }
                        }

                        ignoreParts.ToList().ForEach(ignorePart => ignoredAll.Add(ignorePart));

                        return !ignoredAll.Contains(ragdollPart);
                    }
                    return true;
                })
                .FindAll(ragdollPart => ragdollPart.GetComponent<Collider>() && !ragdollPart.Equals(_ragdollParent))
                .ConvertAll(ragdollPart => ragdollPart.GetComponent<Collider>());

            
            _ragdollParts.ForEach(ragdollPart => _totalMass += ragdollPart.attachedRigidbody.mass);

            foreach (Collider ragdollPart in _ragdollParts)
            {
                float ragdollPartMass = ragdollPart.attachedRigidbody.mass;
                float ratio = ragdollPartMass / _totalMass;

                _massRatios.Add(ragdollPart.transform, ratio);
            }
        }

        _isChecked = true;
    }

    private void OnApplyClicked()
    {
        if (_newMass == 0) return;

        _ragdollParent.GetComponent<Collider>().attachedRigidbody.mass = _newMass;
        _ragdollParts.ForEach(ragdollPart => ragdollPart.attachedRigidbody.mass = _massRatios[ragdollPart.transform] * _newMass);

        ResetVariables();
    }

    private void OnClearClicked()
    {
        ResetVariables();
    }

    private void ResetVariables()
    {
        _ragdollParent = null;
        _ragdollParts = null;

        ignoreParts = null;
        so.Update();

        _massRatios.Clear();
        _newMass = .0f;
        _totalMass = .0f;
        ignoreParts = null;
        _isChecked = false;
    }
}
