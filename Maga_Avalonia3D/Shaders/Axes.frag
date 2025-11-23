#version 300 es
precision highp float;

uniform float axisColorR;
uniform float axisColorG;
uniform float axisColorB;

out vec4 FragColor;

void main()
{
    vec3 axisColor = vec3(axisColorR, axisColorG, axisColorB);
    FragColor = vec4(axisColor, 1.0);
}