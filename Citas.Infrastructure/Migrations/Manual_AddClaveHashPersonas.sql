-- Modulo de Citas: login global del cliente.
-- Agrega la columna de hash de contrasena a la tabla manual "personas".
-- Idempotente y seguro. Correr en la BD de Citas (Postgres).

ALTER TABLE public.personas
    ADD COLUMN IF NOT EXISTS "ClaveHash" varchar NULL;

-- Opcional (recomendado): evitar correos duplicados a nivel de BD.
-- Solo aplica si hoy no hay emails repetidos.
-- CREATE UNIQUE INDEX IF NOT EXISTS ux_personas_email
--     ON public.personas (lower(email)) WHERE email IS NOT NULL;
