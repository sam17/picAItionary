import { Pencil } from 'lucide-react';

export const GameTitle: React.FC = () => {
  return (
    <div className="flex items-center justify-center gap-2 mb-4">
      <h1 className="text-3xl sm:text-4xl font-bold text-center bg-gradient-to-r from-blue-500 to-purple-500 text-transparent bg-clip-text animate-gradient">
        PicAItionary
      </h1>
      <Pencil className="w-8 h-8 text-blue-500 animate-bounce" />
    </div>
  );
}; 