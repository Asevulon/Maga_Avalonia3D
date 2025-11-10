#version 300 es

precision highp float;

uniform float colorR;
uniform float colorG;
uniform float colorB;

out vec4 FragColor;

void main()
{
    FragColor = vec4(colorR, colorG, colorB, 1.0);
}