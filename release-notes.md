## v1.2.1 — Auditoria de Segurança

### Segurança
- Logs não registram mais query strings OAuth, tokens ou e-mail
- Import de configuração com validação de tamanho/formato/conteúdo + gravação atômica + rollback
- Extração de ZIP bloqueia caminhos fora da pasta de destino (Zip Slip)
- Downloads usam HTTPS, limite de tamanho e SHA-256 fixo
- Fleasion e NVIDIA Profile Inspector reverificados antes de executar
- Página Mobile não baixa instaladores automaticamente (só link oficial)
- Temas/XAML remotos mutáveis deixaram de ser baixados
- Backups rejeitam nomes com path traversal
- Abertura de links externos restrita a HTTP/HTTPS sem concatenação insegura

### Correções
- Logger, polling de canais e wait de processos corrigidos (races, dispose, waits infinitas)
- Autosave de plugins com limite de tamanho e quantidade
