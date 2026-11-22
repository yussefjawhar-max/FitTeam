using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using FitTeam.Core;
namespace FitTeam.Desktop.Controls;

public sealed class ExerciseIllustration : Control
{
    public string Movement {get;set;}="";
    public double Seconds {get;set;}
    public ExerciseIllustration(){Height=270;MinWidth=300;ClipToBounds=true;}
    public override void Render(DrawingContext context)
    {
        base.Render(context);context.DrawRectangle(Brush.Parse("#EDF7F5"),null,new Rect(Bounds.Size),14,14);
        if(HumanExerciseSprites.Draw(context,new Rect(Bounds.Size),Movement,Seconds))return;
        if(Movement.Length==0)return;
        double[] standing=[0,20,0,45,0,105,-20,75,-22,112,20,75,22,112,-18,145,-22,185,18,145,22,185];
        double[] first=standing.ToArray(),last=standing.ToArray();
        switch(Movement)
        {
            case "squat":last=[-20,75,-20,100,20,142,-45,104,-70,104,-40,109,-65,109,-15,147,-22,185,18,147,22,185];break;
            case "pushup":first=[-70,105,-50,125,20,140,-45,150,-45,185,-55,150,-55,185,45,185,75,160,45,185,80,155];last=[-70,145,-50,160,20,160,-70,175,-45,185,-75,175,-55,185,45,185,75,160,45,185,80,155];break;
            case "plank":first=[-75,106,-53,127,20,149,-52,171,-78,183,-45,170,-67,184,59,165,96,185,59,165,100,185];last=first.ToArray();break;
            case "bridge":first=[-85,168,-62,181,0,183,-45,180,-12,183,-43,181,-15,185,35,133,65,185,45,134,73,185];last=first.ToArray();last[4]=0;last[5]=135;break;
            case "birddog":first=[-72,101,-53,124,35,140,-55,155,-60,185,-45,155,-50,185,36,180,67,185,46,180,76,185];last=first.ToArray();last[6]=-80;last[7]=126;last[8]=-116;last[9]=123;last[18]=85;last[19]=140;last[20]=124;last[21]=137;break;
            case "deadbug":first=[-87,170,-64,184,0,185,-60,140,-62,100,-53,140,-50,100,36,138,8,103,44,138,17,104];last=first.ToArray();last[6]=-88;last[7]=180;last[8]=-120;last[9]=181;last[18]=54;last[19]=181;last[20]=107;last[21]=185;break;
            case "press":first[6]=-38;first[7]=65;first[8]=-40;first[9]=40;first[10]=38;first[11]=65;first[12]=40;first[13]=40;last[6]=-26;last[7]=25;last[8]=-25;last[9]=-8;last[10]=26;last[11]=25;last[12]=25;last[13]=-8;break;
            case "raise":last[6]=-47;last[7]=49;last[8]=-83;last[9]=51;last[10]=47;last[11]=49;last[12]=83;last[13]=51;break;
            case "curl":last[6]=-20;last[7]=76;last[8]=-25;last[9]=42;last[10]=20;last[11]=76;last[12]=25;last[13]=42;break;
            case "calf":last=standing.Select((v,i)=>i%2==1&&i is not (17 or 21)?v-14:v).ToArray();break;
            case "row":first=[-49,63,-31,89,18,129,-42,123,-45,161,-28,123,-30,161,22,152,8,185,48,151,61,185];last=first.ToArray();last[6]=14;last[7]=102;last[8]=-26;last[9]=116;last[10]=21;last[11]=111;last[12]=-17;last[13]=124;break;
            case "pulldown":first[6]=-37;first[7]=26;first[8]=-42;first[9]=-6;first[10]=37;first[11]=26;first[12]=42;first[13]=-6;last[6]=-45;last[7]=73;last[8]=-25;last[9]=52;last[10]=45;last[11]=73;last[12]=25;last[13]=52;break;
            case "bench":case "fly":first=[-80,141,-58,160,16,164,-37,126,-24,101,-52,123,-43,99,60,165,68,185,62,166,79,185];last=first.ToArray();if(Movement=="bench"){last[6]=-42;last[7]=100;last[8]=-45;last[9]=53;last[10]=-23;last[11]=103;last[12]=-16;last[13]=53;}else{last[6]=-74;last[7]=136;last[8]=-110;last[9]=118;last[10]=-10;last[11]=130;last[12]=33;last[13]=107;}break;
            case "legpress":first=[-70,70,-53,91,-27,145,-43,123,-24,157,-49,124,-30,157,25,109,4,75,34,118,9,85];last=first.ToArray();last[14]=22;last[15]=107;last[16]=61;last[17]=57;last[18]=31;last[19]=115;last[20]=72;last[21]=65;break;
            case "cycle":first=[-50,35,-31,60,24,112,-58,81,-78,105,-45,84,-69,106,1,143,28,175,60,144,24,145];last=first.ToArray();last[14]=50;last[15]=134;last[16]=24;last[17]=145;last[18]=6;last[19]=149;last[20]=29;last[21]=175;break;
            case "walk":case "run":case "warmup":first[6]=-37;first[7]=62;first[8]=-67;first[9]=45;first[10]=39;first[11]=86;first[12]=68;first[13]=111;first[14]=-40;first[15]=147;first[16]=-72;first[17]=185;first[18]=35;first[19]=123;first[20]=64;first[21]=145;last=first.Select((v,i)=>i%2==0?-v:v).ToArray();break;
            case "football":last[18]=55;last[19]=132;last[20]=95;last[21]=156;break;
            case "basketball":last[10]=42;last[11]=86;last[12]=42;last[13]=122;break;
            case "volleyball":last[6]=-40;last[7]=23;last[8]=-25;last[9]=-6;last[10]=45;last[11]=44;last[12]=40;last[13]=5;break;
        }
        double fraction=(Math.Sin(Seconds*1.8-Math.PI/2)+1)/2;
        double scale=Math.Min((Bounds.Width-50)/280,(Bounds.Height-42)/215);double cx=Bounds.Width/2,cy=Bounds.Height-210*scale-12;
        Point P(double x,double y)=>new(cx+x*scale,cy+y*scale);
        void Figure(double[] pose,IBrush color,double opacity)
        {
            using(context.PushOpacity(opacity))
            {
                var pen=new Pen(color,6*scale);
                void Line(int a,int b)=>context.DrawLine(pen,P(pose[a*2],pose[a*2+1]),P(pose[b*2],pose[b*2+1]));
                context.DrawEllipse(color,null,P(pose[0],pose[1]),12*scale,12*scale);
                Line(0,1);Line(1,2);Line(1,3);Line(3,4);Line(1,5);Line(5,6);Line(2,7);Line(7,8);Line(2,9);Line(9,10);
                context.DrawLine(new Pen(Ui.Green,12*scale),P(pose[2],pose[3]),P(pose[4],pose[5]));
            }
        }
        var equipment=new Pen(Ui.Muted,3*scale);context.DrawLine(new Pen(Brush.Parse("#C9DDD9"),2),P(-130,190),P(130,190));
        if(Movement is "squat" or "bench" or "fly")context.DrawRectangle(Brush.Parse("#B5CCC7"),null,new Rect(P(Movement=="squat"?10:-66,Movement=="squat"?156:171),new Size((Movement=="squat"?50:135)*scale,9*scale)));
        if(Movement=="cycle"){context.DrawEllipse(null,equipment,P(-70,165),34*scale,34*scale);context.DrawEllipse(null,equipment,P(70,165),34*scale,34*scale);context.DrawLine(equipment,P(-70,165),P(22,119));context.DrawLine(equipment,P(22,119),P(70,165));context.DrawLine(equipment,P(-70,165),P(70,165));}
        Figure(last,Ui.Muted,0.16);
        double[] current=first.Select((v,i)=>v+(last[i]-v)*fraction).ToArray();Figure(current,Ui.Ink,1);
        if(Movement is "curl" or "press" or "raise" or "row" or "bench" or "fly")foreach(int hand in new[]{4,6})context.DrawRectangle(Ui.Muted,null,new Rect(P(current[hand*2]-12,current[hand*2+1]-4),new Size(24*scale,8*scale)),3,3);
        if(Movement is "football" or "basketball" or "volleyball")
        {
            double x=Movement=="football"?48+55*fraction:Movement=="basketball"?49:-34;
            double y=Movement=="football"?177:Movement=="basketball"?140+37*fraction:15-18*fraction;
            context.DrawEllipse(Brush.Parse(Movement=="basketball"?"#DB9C47":"#FAFEFD"),equipment,P(x,y),12*scale,12*scale);
        }
    }
}
