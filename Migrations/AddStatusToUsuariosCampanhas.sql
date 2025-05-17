-- Verificar se a coluna já existe antes de adicioná-la
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_name = 'usuarios_campanhas' 
        AND column_name = 'status'
    ) THEN
        -- Adicionar a coluna status
        ALTER TABLE usuarios_campanhas ADD COLUMN status VARCHAR(255) DEFAULT 'Pendente';
        
        -- Adicionar um comentário explicativo
        COMMENT ON COLUMN usuarios_campanhas.status IS 'Status da participação do usuário na campanha: Pendente, Completa, etc.';
    END IF;
END
$$; 