#version 300 es
precision highp float;

// ИЗМЕНЕНО: три отдельных uniform float вместо одного vec3
uniform float axisColorR;
uniform float axisColorG;
uniform float axisColorB;

out vec4 FragColor;

void main()
{
    // ИЗМЕНЕНО: собираем цвет из трех компонентов
    vec3 axisColor = vec3(axisColorR, axisColorG, axisColorB);
    FragColor = vec4(axisColor, 1.0);
}