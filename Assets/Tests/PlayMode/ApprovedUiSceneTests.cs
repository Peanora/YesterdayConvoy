using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace YesterdayConvoy.Tests
{
    public sealed class ApprovedUiSceneTests
    {
        [Test]public void ThreeRealStaticFontsLoadWithChineseDigitsAndNoVariableFontTable()
        {
            var fonts=new Font[3];string[] weights={"Regular","Semibold","Bold"};
            for(int n=0;n<weights.Length;n++)
            {
                string name="YesterdayConvoyUI-"+weights[n];fonts[n]=Resources.Load<Font>("UI/"+name);Assert.That(fonts[n],Is.Not.Null,name);
                foreach(char glyph in "商店备战羁绊霜星0123456789+-/()")Assert.That(fonts[n].HasCharacter(glyph),Is.True,name+" missing "+glyph);
                // PlayMode runs in the Editor: inspect the actual imported source, not a fallback font name.
                string path=Path.Combine(Application.dataPath,"Fonts/Resources/UI/"+name+".ttf");Assert.That(File.Exists(path),Is.True,path);
                byte[] data=File.ReadAllBytes(path);Assert.That(data.Length,Is.GreaterThan(12));int tables=data[4]*256+data[5];
                Assert.That(data.Length,Is.GreaterThanOrEqualTo(12+tables*16));
                for(int i=0;i<tables;i++)Assert.That(Encoding.ASCII.GetString(data,12+i*16,4),Is.Not.EqualTo("fvar"),name+" must be a static font.");
            }
            Assert.That(fonts.Distinct().Count(),Is.EqualTo(3),"Weights must load separate real font assets.");
        }
    }
}
