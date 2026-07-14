-- Function PostgreSQL para inserção de log de processo.
-- Insere um registro de log de processo e retorna o ID gerado.

CREATE OR REPLACE FUNCTION sta.fn_inclui_log_processo(
    p_cd_alias_sistema VARCHAR,
    p_cn_processo INT,
    p_dt_inicio TIMESTAMP,
    p_id_status_processo CHAR(1),
    p_qt_registros_processados BIGINT DEFAULT 0,
    p_vl_registros_processados BIGINT DEFAULT 0,
    p_qt_registros_erro BIGINT DEFAULT 0,
    p_vl_registros_erro BIGINT DEFAULT 0,
    p_xml_obs_processo TEXT DEFAULT NULL
) RETURNS INT AS $$
DECLARE
    v_cn_sistema INT;
    v_cn_log INT;
BEGIN
    -- Buscar o sistema pelo alias
    SELECT cn_sistema INTO v_cn_sistema
    FROM sta.tbl_sistema
    WHERE cd_alias_sistema = p_cd_alias_sistema;

    IF v_cn_sistema IS NULL THEN
        RAISE EXCEPTION 'Sistema com alias % não encontrado.', p_cd_alias_sistema;
    END IF;

    -- Inserir log com dt_fim_processo = NOW()
    INSERT INTO sta.tbl_log_processo (
        cn_sistema,
        cn_processo,
        dt_inicio,
        dt_fim_processo,
        id_status_processo,
        qt_registros_processados,
        vl_registros_processados,
        qt_registros_erro,
        vl_registros_erro,
        xml_obs_processo
    ) VALUES (
        v_cn_sistema,
        p_cn_processo,
        p_dt_inicio,
        NOW(),
        p_id_status_processo,
        p_qt_registros_processados,
        p_vl_registros_processados,
        p_qt_registros_erro,
        p_vl_registros_erro,
        p_xml_obs_processo
    ) RETURNING cn_log_processo INTO v_cn_log;

    RETURN v_cn_log;
END;
$$ LANGUAGE plpgsql;
