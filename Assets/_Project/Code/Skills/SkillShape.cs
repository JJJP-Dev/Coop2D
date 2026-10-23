// The area a skill hits (#162). Saved in the skill assets as a number: add new shapes at the end.
public enum SkillShape
{
    // A circle where you aim, up to the cast range: Estallido, Marca, Llovizna, Cortocircuito.
    Circle,

    // A straight band from the caster towards the cursor that hits everything in it: Chispa.
    Line,

    // A wedge from the caster towards the cursor: Ola.
    Cone,
}
