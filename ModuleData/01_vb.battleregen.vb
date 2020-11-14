' I'm not as well-versed in VB as I'm in C#. Apologies if it look bad.
Imports BattleRegen
Imports System
Imports TaleWorlds.MountAndBlade

Namespace BattleRegen.Formulas
    ' Sine regen formula - credit: WyrdOh (https://forums.nexusmods.com/index.php?showtopic=8702373/#entry86794963)
    ' Rewritten in VB as an example. I personally prefer C#, but VB works.
    Public NotInheritable Class SineFormula
        Inherits Formula
        Public Overrides ReadOnly Property Name As String = "{=BattleRegen_Sine}Sine"

        Public Overrides ReadOnly Property Id As String = "03_Sine"

        ' Built-in values must be loaded first. Sine should be right behind EveOnline.
        Public Overrides ReadOnly Property Priority As Integer = Integer.MinValue

        Public Overrides Function Calculate(agent As Agent, regenRate As Double, regenTime As Double) As Double
            Dim ratio As Double = agent.Health / agent.HealthLimit
            Return 2.5 * regenRate * Math.Sin(Math.PI / 2 * ratio)
        End Function
    End Class
End Namespace
