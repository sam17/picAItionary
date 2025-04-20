import { Hand, Eye, Square, Circle, Dot, Triangle } from 'lucide-react';

interface DiceProps {
  roll: number;
  modifier: string;
}

const modifierIcons = {
  1: <Hand className="w-8 h-8" />,
  2: <Eye className="w-8 h-8" />,
  3: <Square className="w-8 h-8" />,
  4: <Circle className="w-8 h-8" />,
  5: <Dot className="w-8 h-8" />,
  6: <Triangle className="w-8 h-8" />,
};

export const Dice: React.FC<DiceProps> = ({ roll, modifier }) => {
  return (
    <div className="relative w-24 h-24 mb-4">
      <div className="absolute w-full h-full bg-white rounded-xl shadow-lg border-4 border-green-500 transform rotate-3 transition-transform duration-300 hover:rotate-6 animate-[roll_1s_ease-out]">
        <div className="w-full h-full p-3">
          <div className="w-full h-full flex items-center justify-center">
            <div className="text-green-500 animate-[pop_0.3s_ease-out_0.7s_both]">
              {modifierIcons[roll as keyof typeof modifierIcons]}
            </div>
          </div>
        </div>
      </div>
      <div className="absolute w-full h-full bg-green-500/20 rounded-xl -z-10 transform -rotate-3 transition-transform duration-300 hover:-rotate-6 animate-[roll-shadow_1s_ease-out]" />
    </div>
  );
}; 