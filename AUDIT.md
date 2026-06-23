# Hercules — auditoria técnica

Data: 2026-06-23

## Resultado desta revisão

- Builds Debug e Release concluídos com zero erros.
- A consulta atual ao índice oficial do NuGet não encontrou pacotes vulneráveis, incluindo dependências transitivas.
- Nenhum arquivo de workflow ou configuração do GitHub foi alterado nesta revisão.
- Importação de configurações agora valida tamanho, formato e conteúdo, grava atomicamente e restaura todos os arquivos se qualquer etapa falhar.
- Extração de ZIPs bloqueia caminhos fora da pasta de destino (Zip Slip).
- Downloads de executáveis e pacotes sensíveis usam HTTPS, limite de tamanho e SHA-256 fixo.
- Fleasion e NVIDIA Profile Inspector são verificados novamente antes da execução, inclusive quando já existem no disco.
- Downloads de notícias, ícones, músicas e recursos de skybox possuem limites de memória/disco e validação básica de conteúdo ou integridade.
- A página Mobile não baixa nem executa mais instaladores de terceiros automaticamente; abre apenas a página oficial após ação explícita.
- Temas/XAML remotos mutáveis deixaram de ser baixados automaticamente.
- Logs não registram mais query strings OAuth, tokens ou e-mail da conta.
- Backups rejeitam nomes com travessia de diretório.
- Logger, polling de canais e espera por processos foram corrigidos para evitar corridas, descarte incorreto e esperas infinitas.
- Autosave de plugins limita quantidade e tamanho das entradas do ZIP.
- Abertura de links externos restringe links web a HTTP/HTTPS e passa argumentos ao fallback do Windows sem concatenação insegura.

## Verificação

- `dotnet build Hercules/Hercules.csproj --no-restore`: aprovado, 0 erros.
- `dotnet build Hercules/Hercules.csproj -c Release --no-restore`: aprovado, 0 erros.
- `git diff --check`: aprovado após a correção de whitespace.
- O projeto ainda emite 509 warnings no build Release. Muitos vêm do `wpfui` incorporado; os avisos restantes incluem nulabilidade, membros não usados e código legado de UI/overlays.
- Não existe projeto de testes automatizados na solução; isso continua sendo a maior lacuna de validação.

## Continuação em 2026-06-23

- Reduzi os warnings Release de 619 para 509 sem alterar workflows do GitHub.
- Corrigi contratos `INotifyPropertyChanged` para eventos anuláveis onde o padrão do .NET exige `PropertyChangedEventHandler?`.
- Reforcei a presença do Discord para tolerar assets, thumbnails, nomes e localização ausentes, usando fallback seguro em vez de possível exceção.
- Corrigi possíveis nulos em importação de perfis NVIDIA, registro de associação do Windows e watcher de limite de CPU.
- Marquei chamadas assíncronas intencionalmente fire-and-forget com descarte explícito ou `await`, reduzindo ruído e deixando intenção clara.
- Protegi diretórios de autosave/sessão de plugins quando `Path.GetDirectoryName(...)` retorna nulo.

## Próximas funções sugeridas

1. Central de integridade: verificar arquivos, integrações e configurações instaladas, com botão de reparo seguro.
2. Backup versionado: snapshots antes de mudanças em mods/FastFlags e restauração com comparação visual.
3. Diagnóstico exportável: relatório sanitizado de versão, arquivos ausentes, permissões, rede e falhas recentes.
4. Catálogo assinado de extensões: manifesto com hash, versão, origem e permissões antes da instalação.
5. Perfis por jogo/conta: FastFlags, mods e atalhos aplicados automaticamente por contexto.
6. Modo seguro: iniciar sem plugins, integrações, temas personalizados ou modificações para recuperar instalações quebradas.
7. Atualizador com canal e rollback: estável/beta, assinatura do artefato e retorno automático à versão anterior.
8. Suite de testes: caminhos de arquivo, importação transacional, rejeição de downloads adulterados e migração de JSON.

## Débito técnico restante

- Reduzir warnings gradualmente, começando pelas chamadas assíncronas não aguardadas e possíveis nulos em caminhos de inicialização.
- Substituir `catch` vazios por exceções específicas e logs sanitizados.
- Isolar ou atualizar o fork incorporado de `wpfui` para que os warnings de terceiros não ocultem regressões do Hercules.
- Assinar os executáveis publicados com Authenticode; hash fixo protege downloads conhecidos, mas não substitui assinatura de código.
