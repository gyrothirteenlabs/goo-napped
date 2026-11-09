using System;
using System.Collections.Generic;
using UnityEngine;

// Prototipe: mainkan frame body + face bersamaan (2 SpriteRenderer, animasi sama).
// Arah: front / side (hadap kanan) / back. Kiri = flipX.
public class SlimeSpriteAnimator : MonoBehaviour
{
    [Serializable]
    public class Clip
    {
        public string anim;   // idle, move, eat, jelly, split, merge
        public string dir;    // front, side, back
        public Sprite[] body;
        public Sprite[] face;
    }

    public SpriteRenderer bodyRenderer;
    public SpriteRenderer faceRenderer;
    public List<Clip> clips = new();
    public string anim = "idle";
    public string dir = "front";
    public bool faceLeft;
    public float fps = 8f;

    Clip current;
    float t;

    public void Play(string newAnim, string newDir, bool left)
    {
        if (newAnim == anim && newDir == dir && left == faceLeft && current != null) return;
        bool restart = newAnim != anim;
        anim = newAnim; dir = newDir; faceLeft = left;
        current = Find(anim, dir);
        if (restart) t = 0f;
    }

    Clip Find(string a, string d)
    {
        foreach (var c in clips) if (c.anim == a && c.dir == d) return c;
        foreach (var c in clips) if (c.anim == a) return c;
        return clips.Count > 0 ? clips[0] : null;
    }

    void Update()
    {
        if (current == null || current.anim != anim || current.dir != dir) current = Find(anim, dir);
        if (current == null || current.body == null || current.body.Length == 0) return;

        t += Time.deltaTime * fps;
        int i = (int)t % current.body.Length;

        bodyRenderer.sprite = current.body[i];
        bodyRenderer.flipX = faceLeft;
        if (faceRenderer != null && current.face != null && current.face.Length > i)
        {
            faceRenderer.sprite = current.face[i];
            faceRenderer.flipX = faceLeft;
        }
    }
}
