-- =====================================================
-- Migration: Add email verification tables and functions
-- =====================================================

-- Table for email verification codes (welcome, email change)
CREATE TABLE IF NOT EXISTS public.codigos_verificacao (
    id BIGSERIAL PRIMARY KEY,
    email VARCHAR(255) NOT NULL,
    codigo VARCHAR(8) NOT NULL,
    tipo VARCHAR(50) NOT NULL DEFAULT 'welcome',
    criado_em TIMESTAMP NOT NULL DEFAULT NOW(),
    expira_em TIMESTAMP NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_codigos_verificacao_email ON public.codigos_verificacao(email);
CREATE INDEX IF NOT EXISTS idx_codigos_verificacao_codigo ON public.codigos_verificacao(codigo);

-- Function to insert password recovery code
CREATE OR REPLACE FUNCTION public.inserir_recuperacao_senha(
    p_user_id BIGINT,
    p_codigo VARCHAR,
    p_expires_at TIMESTAMP
)
RETURNS VOID
LANGUAGE plpgsql
SECURITY DEFINER
AS $$
BEGIN
    -- Invalidate previous codes for this user
    UPDATE public.recuperacao_senha
    SET expires_at = NOW()
    WHERE user_id = p_user_id
      AND (expires_at IS NULL OR expires_at > NOW());

    -- Insert new recovery code
    INSERT INTO public.recuperacao_senha (user_id, codigo, expires_at, created_at)
    VALUES (p_user_id, p_codigo, p_expires_at, NOW());
END;
$$;

-- Enable Row Level Security
ALTER TABLE public.codigos_verificacao ENABLE ROW LEVEL SECURITY;

-- Create policy for public insert/select (anonymous users can insert and check codes)
CREATE POLICY "Anyone can insert verification codes"
    ON public.codigos_verificacao
    FOR INSERT
    TO anon
    WITH CHECK (true);

CREATE POLICY "Anyone can view verification codes"
    ON public.codigos_verificacao
    FOR SELECT
    TO anon
    USING (true);

CREATE POLICY "Anyone can delete verification codes"
    ON public.codigos_verificacao
    FOR DELETE
    TO anon
    USING (true);
