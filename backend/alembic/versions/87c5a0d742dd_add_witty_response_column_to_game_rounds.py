"""add witty_response column to game_rounds

Revision ID: 87c5a0d742dd
Revises: 
Create Date: 2025-04-12 16:50:43.769674

"""
from typing import Sequence, Union

from alembic import op
import sqlalchemy as sa


# revision identifiers, used by Alembic.
revision: str = '87c5a0d742dd'
down_revision: Union[str, None] = None
branch_labels: Union[str, Sequence[str], None] = None
depends_on: Union[str, Sequence[str], None] = None


def upgrade() -> None:
    # ### commands auto generated by Alembic - please adjust! ###
    op.add_column('game_rounds', sa.Column('witty_response', sa.String(), nullable=True))
    # ### end Alembic commands ###


def downgrade() -> None:
    # ### commands auto generated by Alembic - please adjust! ###
    op.drop_column('game_rounds', 'witty_response')
    # ### end Alembic commands ###
